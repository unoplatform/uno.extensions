using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Windows.Foundation.Collections;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Logging;
using _CollectionChanged = Uno.Extensions.Collections.RichNotifyCollectionChangedEventArgs;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets
{
	internal class FlatCollectionChangedFacet : CollectionChangedFacet
	{
		private readonly Lazy<ICollectionView> _owner;

		public int Correction => 0;

		public FlatCollectionChangedFacet(Func<ICollectionView> owner)
			: base(owner)
		{
			_owner = new Lazy<ICollectionView>(owner, LazyThreadSafetyMode.None);
		}

		public void AddChild(CollectionChangedFacet item, IObservableVector<object?> source, IObservableCollectionSnapshot? currentItems = null)
		{
			item.AddVectorChangedHandler(OnVectorChanged, lowPriority: true);
			item.AddCollectionChangedHandler(OnCollectionChanged, lowPriority: true);

			var currentItemsCount = currentItems?.Count ?? 0;
			if (currentItemsCount > 0)
			{
				var offset = GetChildOffset(source);

				// As we don't have to compensate the Count on the FlatView, we can raise collection changes here without using the RaiseItemPerItem of the CollectionChangedFacet
				// We can also raise the CollectionChanged out of sync with VectorChanged.

				if (_owner.Value.Log().IsEnabled(LogLevel.Trace)) _owner.Value.Log().Trace($"{_owner.Value.GetType().Name}: As a new group was added, will raise 'Add' @ {offset} of the {currentItemsCount} items  (collection count: {_owner.Value.Count}).");

				CollectionChanged?.Invoke(_CollectionChanged.AddSome(currentItems!, offset));
				if (VectorChanged != null)
				{
					// Note: As weird as it seems, on windows we MUST raise the 'ItemInserted' in descending order! (Otherwise the app will crash ...) 
					for (var i = currentItemsCount + offset - 1; i >= offset; i--)
					{
						VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)i));
					}
				}
			}
		}

		public void RemoveChild(CollectionChangedFacet item, IObservableVector<object> source, IObservableCollectionSnapshot? currentItems = null)
		{
			item.RemoveVectorChangedHandler(OnVectorChanged, lowPriority: true);
			item.RemoveCollectionChangedHandler(OnCollectionChanged, lowPriority: true);

			var currentItemsCount = currentItems?.Count ?? 0;
			if (currentItemsCount > 0)
			{
				var offset = GetChildOffset(source);

				// As we don't have to compensate the Count on the FlatView, we can raise collection changes here without using the RaiseItemPerItem of the CollectionChangedFacet
				// We can also raise the CollectionChanged out of sync with VectorChanged.

				if (_owner.Value.Log().IsEnabled(LogLevel.Trace)) _owner.Value.Log().Trace($"{_owner.Value.GetType().Name}: As a group was removed, will raise 'Remove' @ {offset} of the {currentItemsCount} items  (collection count: {_owner.Value.Count}).");

				CollectionChanged?.Invoke(_CollectionChanged.RemoveSome(currentItems!, offset));
				if (VectorChanged != null)
				{
					// Note: As weird as it seems, on windows we MUST raise the 'ItemRemoved' in descending order! (Otherwise the app will crash ...) 
					for (var i = currentItemsCount + offset - 1; i >= offset; i--)
					{
						VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint)i));
					}
				}
			}
		}

		/// <summary>
		/// Be aware that this method only raise a reset event, you still have to invoke remove and add for items!
		/// </summary>
		public void NotifyReset()
		{
			CollectionChanged?.Invoke(Uno.Extensions.Collections.CollectionChanged.Reset());
			VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.Reset, 0));
		}

		private void OnVectorChanged(IObservableVector<object?> sender, IVectorChangedEventArgs args)
		{
			var vectorChanged = VectorChanged;
			if (vectorChanged is null)
			{
				return;
			}

			var offset = GetChildOffset(sender);
			vectorChanged(new VectorChangedEventArgs(args.CollectionChange, (uint)(args.Index + offset)));
		}

		private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
		{
			var collectionChanged = CollectionChanged;
			if (collectionChanged is null)
			{
				return;
			}

			var offset = GetChildOffset(sender!); // ! => Cannot be null for the kind of collection we use
			collectionChanged(Uno.Extensions.Collections.CollectionChanged.Offset(args, offset));
		}

		private int GetChildOffset(object child)
		{
			var offset = 0;
			foreach (ICollectionViewGroup item in _owner.Value.CollectionGroups)
			{
				if (child == item.GroupItems)
				{
					return offset;
				}
				else
				{
					offset += item.GroupItems.Count;
				}
			}

			throw new InvalidOperationException("The provided group does not appear in the source.");
		}
	}
}
