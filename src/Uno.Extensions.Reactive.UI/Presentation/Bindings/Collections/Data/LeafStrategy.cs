using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Views;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data
{
	/// <summary>
	/// A holder of a leaf in a tree of nested <see cref="IObservableCollection"/>, or the root collection in case on non grouped collections
	/// </summary>
	internal class LeafStrategy : IBindableCollectionDataLayerStrategy
	{
		private readonly DataStructure _dataStructure;
		private readonly CollectionAnalyzer _diffAnalyzer;
		private readonly bool _isRoot;

		public LeafStrategy(DataStructure dataStructure, CollectionAnalyzer diffAnalyzer, bool isRoot)
		{
			_dataStructure = dataStructure;
			_diffAnalyzer = diffAnalyzer;
			_isRoot = isRoot;
		}

		public (CollectionFacet source, ICollectionView view, IEnumerable<object> facets) CreateView(IBindableCollectionViewSource source)
		{
			var view = default(ICollectionView);

			var collectionChangedFacet = new CollectionChangedFacet(() => view ?? throw new InvalidOperationException("The owner provider must be resolved lazily!"));
			var collectionFacet = new CollectionFacet(collectionChangedFacet);
			var extendedPropertiesFacet = new BindableCollectionExtendedProperties();

			if (_isRoot) // I.e. Collection is not grouped
			{
				var paginationFacet = new PaginationFacet(source, extendedPropertiesFacet);
				var selectionFacet = new SelectionFacet(source, () => view ?? throw new InvalidOperationException("The owner provider must be resolved lazily!"));

				view = new BasicView(collectionFacet, collectionChangedFacet, extendedPropertiesFacet, selectionFacet, paginationFacet);

				return (collectionFacet, view, new object[] { collectionFacet, collectionChangedFacet, paginationFacet, selectionFacet, extendedPropertiesFacet });
			}
			else
			{
				view = new BasicView(collectionFacet, collectionChangedFacet, extendedPropertiesFacet);

				return (collectionFacet, view, new object[] { collectionFacet, collectionChangedFacet, extendedPropertiesFacet });
			}
		}

		public IUpdateContext CreateUpdateContext(VisitorType type, TrackingMode mode)
			=> _dataStructure.CreateUpdateContext(type, mode);

		public ILayerTracker GetTracker(IBindableCollectionViewSource source, IUpdateContext context)
		{
			// Use the SelectionFacet of the source if this is a flat view, or the parent if this is the leaf of a nested view
			// NOTE: we do support only one layer of grouping here. We should get the SelectionFacet of the root collection
			//		 cf. https://github.com/unoplatform/uno.extensions/issues/371
			var visitor = new SelectionVisitor((_isRoot ? source : source.Parent!).GetFacet<SelectionFacet>());
			var analyzer = _diffAnalyzer;
			var tracker = new DataLayerTracker(context, analyzer, visitor);

			return tracker;
		}

		public IBindableCollectionDataLayerStrategy CreateSubLayer()
			=> throw new NotSupportedException("You cannot add a layer to a leaf");
	}
}
