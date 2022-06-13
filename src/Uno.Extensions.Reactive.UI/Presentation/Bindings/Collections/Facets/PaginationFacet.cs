using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Logging;
using Uno.Threading;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets
{
	internal interface IPaginationService
	{
		event EventHandler StateChanged;

		bool HasMoreItems { get; }

		bool IsLoadingMoreItems { get; }

		Task<uint> LoadMoreItems(uint count, CancellationToken ct);
	}


	internal sealed class PaginationService : IPaginationService, IDisposable
	{
		private readonly AsyncFunc<uint, uint> _loadMore;

		private LoadRequest? _pending;
		private bool _hasMoreItems;
		private bool _isLoadingMoreItems;
		private bool _isDisposed;

		/// <inheritdoc />
		public event EventHandler? StateChanged;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="loadMore"></param>
		/// <remarks>This service ensure that only one <paramref name="loadMore"/> will be active at once.</remarks>
		public PaginationService(AsyncFunc<uint, uint> loadMore)
		{
			_loadMore = loadMore;
			HasMoreItems = true;
		}

		/// <inheritdoc />
		public bool HasMoreItems
		{
			get => _hasMoreItems;
			private set
			{
				if (_hasMoreItems != value)
				{
					_hasMoreItems = value;
					StateChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		/// <inheritdoc />
		public bool IsLoadingMoreItems
		{
			get => _isLoadingMoreItems;
			private set
			{
				if (_isLoadingMoreItems != value)
				{
					_isLoadingMoreItems = value;
					StateChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		/// <inheritdoc />
		public async Task<uint> LoadMoreItems(uint count, CancellationToken ct)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(PaginationService));
			}

			// No matter the count, we coerce the request to get more items so the source will have only one running load request.
			var request = _pending;
			if (request is null)
			{
				Interlocked.CompareExchange(ref _pending, new LoadRequest(this, count), null);

				request = _pending; // Make sure to get the winning request!
			}

			if (_isDisposed) // We have been disposed while we where starting a new request ...
			{
				request?.Dispose();
				throw new ObjectDisposedException(nameof(PaginationService));
			}

			return await request.GetResult(ct);
		}

		private class LoadRequest : IDisposable
		{
			private readonly CancellationTokenSource _ct = new();
			private readonly FastAsyncLock _gate = new();
			private readonly PaginationService _owner;
			private readonly uint _count;

			private Task<uint>? _task;
			private int _awaiters;

			public LoadRequest(PaginationService owner, uint count)
			{
				_owner = owner;
				_count = count;
			}

			public async Task<uint> GetResult(CancellationToken ct)
			{
				Interlocked.Increment(ref _awaiters);

				if (_task is null)
				{
					using (await _gate.LockAsync(ct).ConfigureAwait(false))
					{
						if (_task is null)
						{
							_owner.IsLoadingMoreItems = true;
							_task = _owner._loadMore(_count, _ct.Token).AsTask();
						}
					}
				}

				var isActive = 1;
				var tokenReg = ct.Register(TryDecrement);
				try
				{
					return await _task.ConfigureAwait(false);
				}
				finally
				{
					TryDecrement();

					tokenReg.Dispose();
				}

				void TryDecrement()
				{
					if (Interlocked.CompareExchange(ref isActive, 0, 1) is 1
						&& Interlocked.Decrement(ref _awaiters) <= 1)
					{
						Dispose();
					}
				}
			}

			/// <inheritdoc />
			public void Dispose()
			{
				_awaiters = -32768;
				_owner.IsLoadingMoreItems = false;

				Interlocked.CompareExchange(ref _owner._pending, null, this);
				_ct.Cancel(throwOnFirstException: false);
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_isDisposed = true;
			_pending?.Dispose();
		}
	}

	internal class SingletonServiceProvider : IServiceProvider, IAsyncDisposable
	{
		private object[] _services;

		public SingletonServiceProvider(params object[] services)
		{
			_services = services;
		}

		/// <inheritdoc />
		public object? GetService(Type serviceType)
			=> _services.FirstOrDefault(serviceType.IsInstanceOfType);

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			foreach (var service in Interlocked.Exchange(ref _services, Array.Empty<object>()))
			{
				switch (service)
				{
					case IAsyncDisposable asyncDisposable:
						await asyncDisposable.DisposeAsync();
						break;

					case IDisposable disposable:
						disposable.Dispose();
						break;
				}
			}
		}
	}


	/// <summary>
	/// The pagination facet of an ICollectionView
	/// </summary>
	internal class PaginationFacet : IDisposable
	{
		internal static IAsyncOperation<LoadMoreItemsResult> EmptyResult { get; } = Task.FromResult(default(LoadMoreItemsResult)).AsAsyncOperation();

		private readonly CancellationTokenSource _ct = new();
		private readonly IPaginationService? _service;
		private readonly BindableCollectionExtendedProperties _properties;

		private bool _hasMoreItems;

		public PaginationFacet(IBindableCollectionViewSource source, BindableCollectionExtendedProperties properties)
		{
			_service = source.GetService(typeof(IPaginationService)) as IPaginationService;
			_properties = properties;

			if (_service is not null)
			{
				_service.StateChanged += OnServiceStateChanged;
				OnServiceStateChanged(_service, EventArgs.Empty);
			}
		}

		public bool HasMoreItems
		{
			get => _hasMoreItems;
			private set
			{
				if (_hasMoreItems != value)
				{
					_hasMoreItems = value;
					_properties.HasMoreItems = value;
				}
			}
		}

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint requestedCount)
		{
			if (!HasMoreItems)
			{
				return EmptyResult;
			}

			return Task
				.Run(
					async () =>
					{
						var resultCount = await _service!.LoadMoreItems(requestedCount, _ct.Token);

						return new LoadMoreItemsResult { Count = resultCount };
					},
					_ct.Token)
				.AsAsyncOperation();
		}

		private void OnServiceStateChanged(object? sender, EventArgs _)
		{
			if (sender is IPaginationService svc)
			{
				_properties.HasMoreItems = HasMoreItems = svc.HasMoreItems;
				_properties.IsLoadingMoreItems = svc.IsLoadingMoreItems;
			}
			else
			{
				_properties.HasMoreItems = HasMoreItems = false;
				_properties.IsLoadingMoreItems = false;
			}
		}

		public void Dispose()
		{
			if (_service is not null)
			{
				_service.StateChanged -= OnServiceStateChanged;
				OnServiceStateChanged(null, EventArgs.Empty);
			}
			_ct.Cancel(throwOnFirstException: false);
			_ct.Dispose();
		}
	}
}
