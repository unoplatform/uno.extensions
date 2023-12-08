using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.Extensions.Reactive.Bindings.Collections.Services;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets
{
	/// <summary>
	/// The pagination facet of an ICollectionView
	/// </summary>
	internal class PaginationFacet : IDisposable
	{
		internal static IAsyncOperation<LoadMoreItemsResult> EmptyResult { get; } = Task.FromResult(default(LoadMoreItemsResult)).AsAsyncOperation();

		private readonly CancellationTokenSource _ct = new();
		private readonly IPaginationService? _service;
		private readonly IBindableCollectionViewSource _source;
		private readonly CollectionChangedFacet _changed;
		private readonly BindableCollectionExtendedProperties _properties;

		public PaginationFacet(IBindableCollectionViewSource source, CollectionChangedFacet changed, BindableCollectionExtendedProperties properties)
		{
			_service = source.GetService(typeof(IPaginationService)) as IPaginationService;
			_source = source;
			_changed = changed;
			_properties = properties;

			if (_service is not null)
			{
				_service.StateChanged += OnServiceStateChanged;
				OnServiceStateChanged(_service, EventArgs.Empty);
			}
		}

		public bool HasMoreItems { get; private set; }

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint requestedCount)
		{
			if (!HasMoreItems)
			{
				return EmptyResult;
			}

			return LoadMoreItemsCore(requestedCount).AsAsyncOperation();
		}

		private async Task<LoadMoreItemsResult> LoadMoreItemsCore(uint requestedCount)
		{
			var loadedCount = await Task
				.Run(() => _service!.LoadMoreItems(requestedCount, _ct.Token), _ct.Token)
				.ConfigureAwait(true); // The task must complete on the UI thread!

			return new LoadMoreItemsResult { Count = loadedCount };
		}

		private void OnServiceStateChanged(object? sender, EventArgs _)
		{
			if (sender is IPaginationService svc)
			{
				_properties.HasMoreItems = HasMoreItems = svc.HasMoreItems;
				_properties.IsLoadingMoreItems = svc.IsLoadingMoreItems;
				_source.Dispatcher?.TryEnqueue(() => _changed.PropertyChanged(nameof(HasMoreItems)));
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
