using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets
{
	/// <summary>
	/// The pagination facet of an ICollectionView
	/// </summary>
	internal class PaginationFacet : IDisposable
	{
		internal static IAsyncOperation<LoadMoreItemsResult> EmptyResult { get; } = Task.FromResult(default(LoadMoreItemsResult)).AsAsyncOperation();

		public PaginationFacet(IBindableCollectionViewSource source, BindableCollectionExtendedProperties properties)
		{
		}

		public bool HasMoreItems => false;

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			// https://github.com/unoplatform/uno.extensions/issues/372
			this.Log().Debug("Pagination is not supported yet.");

			return EmptyResult;
		}

		public void Dispose() { }
	}
}
