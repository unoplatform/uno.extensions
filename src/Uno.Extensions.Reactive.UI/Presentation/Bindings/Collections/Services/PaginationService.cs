using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Threading;

namespace Uno.Extensions.Reactive.Bindings.Collections.Services;

/// <summary>
/// A simple pagination service which acts as a push-pull adapter between source and the <see cref="BindableCollection"/>.
/// </summary>
internal sealed class PaginationService : IPaginationService, IDisposable
{
	private readonly FastAsyncLock _gate = new();
	private readonly AsyncFunc<uint, uint> _loadMore;

	private bool _hasMoreItems;
	private bool _isLoadingMoreItems;
	private bool _isDisposed;

	/// <inheritdoc />
	public event EventHandler? StateChanged;

	/// <summary>
	/// Creates a new instance
	/// </summary>
	/// <param name="loadMore">The delegate to invoke to load more items.</param>
	/// <remarks>This service ensure that only one <paramref name="loadMore"/> will be active at once.</remarks>
	public PaginationService(AsyncFunc<uint, uint> loadMore)
	{
		_loadMore = loadMore;
	}

	/// <inheritdoc />
	public bool HasMoreItems
	{
		get => _hasMoreItems;
		set
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
		set
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

		using (await _gate.LockAsync(ct).ConfigureAwait(false))
		{
			return await new LoadRequest(this, count).GetResult(ct).ConfigureAwait(false);
		}
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
					&& Interlocked.Decrement(ref _awaiters) <= 0)
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

			_ct.Cancel(throwOnFirstException: false);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_isDisposed = true;
	}
}
