using System;
using System.Collections;
using System.Linq;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data
{
	internal class GroupsVisitor : ICollectionUpdaterVisitor
	{
		private readonly IUpdateContext _context;
		private readonly ILayerHolder _source;
		private readonly FlatCollectionChangedFacet _flatCollectionChanged;
		private readonly DataLayerCollection _children;

		public GroupsVisitor(
			IUpdateContext context,
			ILayerHolder source,
			FlatCollectionChangedFacet collectionChanged, 
			DataLayerCollection children)
		{
			_context = context;
			_source = source;
			_flatCollectionChanged = collectionChanged;
			_children = children;
		}

		public void AddItem(object item, ICollectionUpdateCallbacks callbacks)
			// Note: As Windows 10.15063 behavior, if we are initializing the holder (_versionType == Initialize), 
			//		 we don't have to send events for the items that are currently in the list.
			=> AddItem(item, callbacks, raiseAddOnFlat: _context.Type != VisitorType.InitializeCollection);

		private void AddItem(object item, ICollectionUpdateCallbacks callbacks, bool raiseAddOnFlat)
		{
			var group = (IObservableGroup)item;

			// 1. Create the holder for the child, which enables the tracking of those group itself.
			//	  ie. Enables the event 'this.View.Groups[X].CollectionChanged'.
			// Note: Currently we support only one level of branch, so we always create a LeafHolder.
			var (holder, initializer) = _source.CreateSubLayer(group, _context);
			var view = new BindableCollectionGroup(group, holder);

			_children.Add(group, view); // Attach the view to the model, so it will be visible to everybody through the converter

			callbacks.Prepend(initializer);
			callbacks.Append(SubscribeToChild);
			callbacks.Append(initializer.ParentAfter); // We must allow the child to unbuffer it change only after having subscribe to it

			void SubscribeToChild()
			{
				// 2. Once the group has been added into the view, ensure propagation of the collection changes from the child to the flat root view 
				//    and raise add for items that are already present in the view.
				//	  ie. Enables 'this.View.CollectionChanged(group.item)'
				// So give 'null' instead of the actual current items.
				var items = raiseAddOnFlat && !_context.HasReachedLimit 
					? initializer.Result
					: null;

				_flatCollectionChanged.AddChild(holder.GetFacet<CollectionChangedFacet>(), holder.View, items);
			}
		}

		public void SameItem(object original, object updated, ICollectionUpdateCallbacks callbacks)
		{
			var oldGroup = (IObservableGroup)original;
			var newGroup = (IObservableGroup)updated;

			// Even if the items are the "Same" it may be another instance, so ensure to make the _children aware of the new instance.
			_children.Update(oldGroup, newGroup);
		}

		public bool ReplaceItem(object original, object updated, ICollectionUpdateCallbacks callbacks)
		{
			var oldGroup = (IObservableGroup)original;
			var newGroup = (IObservableGroup)updated;

			// Get the view of the old item and associate it to the new group
			var view = _children.Update(oldGroup, newGroup);

			// Propagate the update to the child group holder
			var groupUpdate = view.Holder.PrepareUpdate(newGroup, _context);

			// handled: true
			// On a group we don't want to update the group itself, instead we will raise a property changed on the Group property
			// and raise the collection changes for the items of the group (IObservableGroup acts as a proxy).
			callbacks.Append(groupUpdate); // This is 'Append' in order to let the data been updated before raising the events of the initializer
			callbacks.Append(UpdateGroupKey);
			callbacks.Append(groupUpdate.ParentAfter);

			return true;

			void UpdateGroupKey()
			{
				view.UpdateGroup(newGroup); // Update the Key
			}
		}

		public void RemoveItem(object item, ICollectionUpdateCallbacks callbacks)
			=> RemoveItem(item, callbacks, raiseRemoveOnFlat: true);

		private void RemoveItem(object item, ICollectionUpdateCallbacks callbacks, bool raiseRemoveOnFlat)
		{
			var view = _children.Remove((IObservableGroup)item);
			var currentItems = view.Holder.PrepareRemove(); // Disable collection tracking

			callbacks.Prepend(UnsubscribeFromChild);
			callbacks.Append(DisposeHolder);

			void UnsubscribeFromChild()
			{
				// Stop the event propagation from child group to the root flat view
				var items = raiseRemoveOnFlat && !_context.HasReachedLimit
					? currentItems
					: null;

				_flatCollectionChanged.RemoveChild(view.Holder.GetFacet<CollectionChangedFacet>(), view.Holder.View, items);
			}

			void DisposeHolder()
			{
				view.Holder.Dispose();
			}
		}

		public void Reset(IList oldItems, IList newItems, ICollectionUpdateCallbacks callbacks)
		{
			foreach (var item in oldItems)
			{
				RemoveItem(item, callbacks, raiseRemoveOnFlat: false);
			}

			foreach (var item in newItems)
			{
				AddItem(item, callbacks, raiseAddOnFlat: false);
			}
		}
	}
}
