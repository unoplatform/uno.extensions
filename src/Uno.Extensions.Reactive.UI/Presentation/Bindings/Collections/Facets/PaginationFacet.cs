using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets
{
	/// <summary>
	/// The pagination facet of an ICollectionView
	/// </summary>
	internal class PaginationFacet : IDisposable
	{
		internal static IAsyncOperation<LoadMoreItemsResult> EmptyResult { get; } = Task.FromResult(default(LoadMoreItemsResult)).AsAsyncOperation();

		//private readonly IBindableCollectionViewSource _source;
		//private readonly BindableCollectionExtendedProperties _properties;

		//private CancellationDisposable _loadMore = new CancellationDisposable();
		//private ulong _loadRequestId; // Id for debug purpose
		//private int _pendingLoadRequestCount;

		public PaginationFacet(IBindableCollectionViewSource source, BindableCollectionExtendedProperties properties)
		{
			//_source = source;
			//_properties = properties;

			//source.CurrentSourceChanging += (snd, e) =>
			//{
			//	_loadMore.Dispose(); // This will also reset the '_properties.IsLoadingMoreItems'
			//};
			//source.CurrentSourceChanged += (snd, e) =>
			//{
			//	_loadMore = new CancellationDisposable();
			//	_properties.HasMoreItems = HasMoreItems; // We have to update the bindable property
			//};
		}

		public bool HasMoreItems => false;

		//public bool HasMoreItems => _source
		//		.CurrentSource
		//		.Extensions
		//		.OfType<IPaginationExtension>()
		//		.FirstOrDefault()
		//		?.HasMoreItems
		//	?? false;

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			this.Log().Debug("Pagination is not supported yet.");

			return EmptyResult;

			//var requestId = ++_loadRequestId;

			//if(this.Log().IsEnabled(LogLevel.Information)) this.Log().Info($"View is requesting (id: {requestId}) to load {count} items.");

			//var pagination = _source
			//	.CurrentSource
			//	.Extensions
			//	.OfType<IPaginationExtension>()
			//	.FirstOrDefault();

			//if (pagination == null)
			//{
			//	if (this.Log().IsEnabled(LogLevel.Information)) this.Log().Info($"Source does not support pagination, load request {requestId} is ignored.");

			//	return Task
			//		.FromResult(default(LoadMoreItemsResult))
			//		.AsAsyncOperation();
			//}
			//else
			//{
			//	return Task
			//		.Run(async ct =>
			//			{
			//				var request = new LoadMoreItemsRequest(count);

			//				using (ct.Register(request.Aborted))
			//				try
			//				{
			//					// Notify the external listeners that we are loading more items
			//					if (Interlocked.Increment(ref _pendingLoadRequestCount) == 1)
			//					{
			//						_properties.IsLoadingMoreItems = true;
			//					}
			//					_properties.LoadMoreRequested(request); // As this may throw an exception, make sure to do that after the increment and in the try/catch (so the request will still be notified)

			//					// Effectively load the items !
			//					var result = await pagination.LoadMore(ct, count);

			//					if (this.Log().IsEnabled(LogLevel.Information)) this.Log().Info($"Source loaded {result.Count} items for request {requestId} (requested: {count}).");

			//					// Notify listeners of the completion
			//					_properties.HasMoreItems = result.HasMoreItems;
			//					request.Completed(result);

			//					return new LoadMoreItemsResult { Count = result.Count };
			//				}
			//				catch (Exception error)
			//				{
			//					request.Failed(error);

			//					throw;
			//				}
			//				finally
			//				{
			//					if (Interlocked.Decrement(ref _pendingLoadRequestCount) == 0)
			//					{
			//						_properties.IsLoadingMoreItems = false;
			//					}
			//				}
			//			}, 
			//			_loadMore.Token)
			//		.AsAsyncOperation();
			//}
		}

		public void Dispose() { }

		//public void Dispose() => _loadMore?.Dispose();
	}
}
