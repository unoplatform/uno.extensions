using System;
using System.Linq;
using System.Threading;
using Windows.Foundation.Collections;


namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets
{
	/// <summary>
	/// The selection facet of the ICollectionView
	/// </summary>
	internal class SelectionFacet
	{
		/* 
		 * Note: The selection is sync beetween the ListView and the CollectionView only when SelectionMode is 'Single'
		 *		 
		 *		 For modes 'Multiple'and 'Extended' the ListView doesn't notify the CollectionView in any way.
		 *		 
		 *		 When we cycle between the selection modes, only the selection made in 'Single' is kept 
		 *		 (ie. the selection made in other modes is flushed when mode change, and the 'None' doesn't alter the Current on the collection View)
		 *		 
		 *		 When we unselect the 'CurrentItem', list view calls 'MoveCurrentTo(null)'.
		 * 
		 */

		private readonly Lazy<IObservableVector<object>> _target;

		public SelectionFacet(Func<IObservableVector<object>> target)
		{
			_target = new Lazy<IObservableVector<object>>(target, LazyThreadSafetyMode.None);
		}

		private readonly EventRegistrationTokenTable<CurrentChangedEventHandler> _currentChanged = new();
		private readonly EventRegistrationTokenTable<CurrentChangingEventHandler> _currentChanging = new();

		public EventRegistrationToken AddCurrentChangedHandler(CurrentChangedEventHandler value)
			=> _currentChanged.AddEventHandler(value);

#if USE_EVENT_TOKEN
		public void RemoveCurrentChangedHandler(EventRegistrationToken value)
			=> _currentChanged.RemoveEventHandler(value);
#endif

		public void RemoveCurrentChangedHandler(CurrentChangedEventHandler value)
			=> _currentChanged.RemoveEventHandler(value);

		public EventRegistrationToken AddCurrentChangingHandler(CurrentChangingEventHandler value)
			=> _currentChanging.AddEventHandler(value);

#if USE_EVENT_TOKEN
		public void RemoveCurrentChangingHandler(EventRegistrationToken value)
			=> _currentChanging.RemoveEventHandler(value);
#endif

		public void RemoveCurrentChangingHandler(CurrentChangingEventHandler value)
			=> _currentChanging.RemoveEventHandler(value);

		public object? CurrentItem { get; private set; }

		public int CurrentPosition { get; private set; } = -1; // -1 means nothing is selected

		public bool IsCurrentAfterLast => false;

		public bool IsCurrentBeforeFirst => CurrentPosition < 0;

		private bool SetCurrent(int index, object? value, bool isCancelable = true)
		{
			if (CurrentPosition == index && CurrentItem == value)
			{
				// Current is already update to date, do not raise events for nothing!
				return true;
			}

			var changing = _currentChanging.InvocationList;
			if (changing != null)
			{
				var args = new CurrentChangingEventArgs(isCancelable);
				changing.Invoke(this, args);
				if (isCancelable && args.Cancel)
				{
					return false;
				}
			}

			CurrentPosition = index;
			CurrentItem = value;

			_currentChanged.InvocationList?.Invoke(this, CurrentItem);

			return true;
		}

		public bool MoveCurrentTo(object item)
		{
			if (item == null)
			{
				return SetCurrent(-1, null);
			}
			else
			{
				var index = _target.Value.IndexOf(item);

				return index >= 0 && SetCurrent(index, item);
			}
		}

		public bool MoveCurrentToPosition(int index)
		{
			if (index < 0)
			{
				return SetCurrent(-1, null);
			}
			else
			{
				return index < _target.Value.Count && SetCurrent(index, _target.Value[index]);
			}
		}

		public bool MoveCurrentToFirst() => MoveCurrentToPosition(0);

		public bool MoveCurrentToLast() => MoveCurrentToPosition(_target.Value.Count - 1);

		public bool MoveCurrentToNext() => CurrentPosition + 1 < _target.Value.Count && MoveCurrentToPosition(CurrentPosition + 1);

		public bool MoveCurrentToPrevious() => CurrentPosition > 0 && MoveCurrentToPosition(CurrentPosition - 1);
	}
}
