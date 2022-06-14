using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Conversion;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Views;
using TrackingMode = Uno.Extensions.Collections.TrackingMode;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data
{
	/// <summary>
	/// A holder of a branch in a tree of nested <see cref="IObservableCollection"/>.
	/// </summary>
	internal sealed class BranchStrategy : IBindableCollectionDataLayerStrategy
	{
		private readonly DataStructure _dataStructure;
		private readonly uint _dataLevel;
		private readonly CollectionAnalyzer _diffAnalyzer;

		/*
		 * 
		 * Since the update process is a 2 steps process : 
		 *  1. Detect changes on bg thread, and start buffering changes
		 *  2. Raise changes and disable buffering (enable passthrough from source to view)
		 * 
		 * If we don't proactively creates all the group holders, we may have a concurrency issue :
		 *  - We prepare the update from v1 to v2 on a bg thread for realized group A, B but not C (not realized)
		 *  - The view gets the group C, we create it but with v1 since the v2 was not published yet
		 *  - When the v2 is applied (on the UI thread), the group C won't receive the update and will stay on v1
		 * 
		 * This responsability is given to the BranchItemsHolderCollection which prepares and updates itself using 
		 * the ICollectionTrackingCallback.
		 * 
		 */

		public BranchStrategy(DataStructure dataStructure, uint dataLevel, CollectionAnalyzer diffAnalyzer)
		{
			_dataStructure = dataStructure;
			_dataLevel = dataLevel;
			_diffAnalyzer = diffAnalyzer;
		}

		public (CollectionFacet source, ICollectionView view, IEnumerable<object> facets) CreateView(IBindableCollectionViewSource source)
		{
			/*
			 * WARNING - KNOWN LIMITATION ABOUT 'RESET' IN GROUPED COLLECTION:
			 * ---------------------------------------------------------------
			 *
			 * Below we configure the 'groupsCollection' to convert 'reset' as "clear and add", but **we don't do this on the group itself** (i.e. the 'source').
			 *
			 * For instance if we have
			 * List (a.k.a. 'root', if enumerated we get: Item 1, Item 2, Item 3, Item 4)
			 *		Group 1
			 *			Item 1
			 *			Item 2
			 *		Group 2
			 *			Item 3
			 *			Item 4
			 *
			 *	Then if we replace the items of group 2 (i.e. index 1), using a 'Reset'
			 *		Group 2
			 *			Item A
			 *			Item B
			 *
			 * The window's collection (windows 10.15063) view will raise
			 *		root: Reset @ 1
			 *		group[1]: Reset @ 0
			 *
			 * The issue is that the first event makes **absoluteley no sense** (the index is the index of the group while on root we usually deals with item indexes).
			 *
			 * As currently we will do either some add / remove in groups, either a **FULL** reset on the whole grouped collection,
			 * we admit this as limitiation and we won't support this.
			 *
			 * Behavior of full reset in grouped collection is validated by the test 'Given_BindableCollection.When_Grouped_SwitchWithReset'
			 *
			 */

			var view = default(ICollectionView);

			// Extended property bag that can be updated by all facets to expose some extended info of their internal for binding
			// (e.g. Pagination.IsLoadingMoreItems)
			var extendedPropertiesFacet = new BindableCollectionExtendedProperties();

			// Init the flat tracking view (ie. the flatten view of the groups that can be consumed directly by the ICollectionView properties)
			var flatCollectionChanged = new FlatCollectionChangedFacet(() => view ?? throw new InvalidOperationException("The owner provider must be resolved lazily!"));
			var flatSelectionFacet = new SelectionFacet(() => view ?? throw new InvalidOperationException("The owner provider must be resolved lazily!"));
			var flatPaginationFacet = new PaginationFacet(source, extendedPropertiesFacet);

			// Init the groups tracking
			var groupsChanged = new CollectionChangedFacet(() => view?.CollectionGroups ?? throw new InvalidOperationException("The owner provider must be resolved lazily!"));
			var groupsCollection = new CollectionFacet(groupsChanged, convertResetToClearAndAdd: ObservableCollectionKind.Vector, onReseted: flatCollectionChanged.NotifyReset);
			var groupsMappingFacet = new DataLayerCollection();

			// Init the views
			var groupsView = new MapView(
				groupsCollection,
				groupsChanged,
				groupsMappingFacet.ToConverter());
			var flatView = new FlatView(
				source: groupsCollection,
				collectionChangedFacet: flatCollectionChanged,
				selectionFacet: flatSelectionFacet,
				paginationFacet: flatPaginationFacet,
				groups: groupsView);

			// Init the view variable in order to make it accessible to facets
			view = flatView;

			return (groupsCollection, view, new object[] { flatCollectionChanged, flatPaginationFacet, flatSelectionFacet, groupsMappingFacet, extendedPropertiesFacet });
		}

		public IUpdateContext CreateUpdateContext(VisitorType type, TrackingMode mode) 
			=> _dataStructure.CreateUpdateContext(type, mode);

		public ILayerTracker GetTracker(IBindableCollectionViewSource source, IUpdateContext context)
		{
			var groupsMappingFacet = source.GetFacet<DataLayerCollection>();
			var flatCollectionChanged = source.GetFacet<FlatCollectionChangedFacet>();

			// Note: Currently we support only branch of IObservableGroup, so we always create a GroupsVisitor
			var visitor = new GroupsVisitor(context, (ILayerHolder)source, flatCollectionChanged, groupsMappingFacet);
			var analyzer = _diffAnalyzer;
			var tracker = new DataLayerTracker(context, analyzer, visitor);

			return tracker;
		}

		public IBindableCollectionDataLayerStrategy CreateSubLayer()
			=> _dataStructure.GetLayer(_dataLevel + 1);
	}
}
