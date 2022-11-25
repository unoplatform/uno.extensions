﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Foundation.Collections;
using Uno.Extensions.Reactive.Bindings.Collections.Services;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets
{
	/// <summary>
	/// The selection facet of the ICollectionView
	/// </summary>
	internal class SelectionFacet : IDisposable
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

		private readonly EventRegistrationTokenTable<CurrentChangedEventHandler> _currentChanged = new();
		private readonly EventRegistrationTokenTable<CurrentChangingEventHandler> _currentChanging = new();
		private readonly ISelectionService? _service;
		private readonly Lazy<IObservableVector<object>> _target;
		private readonly IDispatcher? _dispatcher;

		private bool _isInit;

		public SelectionFacet(IBindableCollectionViewSource source, Func<IObservableVector<object>> target)
		{
			_service = source.GetService(typeof(ISelectionService)) as ISelectionService;
			_target = new Lazy<IObservableVector<object>>(target, LazyThreadSafetyMode.None);
			_dispatcher = source.Dispatcher;
		}

		private void Init()
		{
			// Note: As the OnServiceStateChanged might cause a SetCurrent, which will try to resolve the _target,
			//		 we must keep this Init lazy.

			if (_isInit)
			{
				return;
			}
			_isInit = true;

			if (_service is not null)
			{
				_service.StateChanged += OnServiceStateChanged;
				OnServiceStateChanged(_service, EventArgs.Empty);
			}
		}

		private void OnServiceStateChanged(object? snd, EventArgs args)
		{
			if (_dispatcher is null or { HasThreadAccess: true })
			{
				MoveCurrentToPosition((int?)_service!.SelectedIndex ?? -1);
			}
			else
			{
				_dispatcher.TryEnqueue(() => MoveCurrentToPosition((int?)_service!.SelectedIndex ?? -1));
			}
		}

		public EventRegistrationToken AddCurrentChangedHandler(CurrentChangedEventHandler value)
		{
			Init();
			return _currentChanged.AddEventHandler(value);
		}

#if USE_EVENT_TOKEN
		public void RemoveCurrentChangedHandler(EventRegistrationToken value)
		{
			Init();
			_currentChanged.RemoveEventHandler(value);
		}
#endif

		public void RemoveCurrentChangedHandler(CurrentChangedEventHandler value)
		{
			Init();
			_currentChanged.RemoveEventHandler(value);
		}

		public EventRegistrationToken AddCurrentChangingHandler(CurrentChangingEventHandler value)
		{
			Init();
			return _currentChanging.AddEventHandler(value);
		}

#if USE_EVENT_TOKEN
		public void RemoveCurrentChangingHandler(EventRegistrationToken value)
		{
			Init();
			_currentChanging.RemoveEventHandler(value);
		}
#endif

		public void RemoveCurrentChangingHandler(CurrentChangingEventHandler value)
		{
			Init();
			_currentChanging.RemoveEventHandler(value);
		}

		public object? CurrentItem { get; private set; }

		public int CurrentPosition { get; private set; } = -1; // -1 means nothing is selected

		public bool IsCurrentAfterLast => false;

		public bool IsCurrentBeforeFirst
		{
			get
			{
				Init();
				return CurrentPosition < 0;
			}
		}

		public bool MoveCurrentTo(object item)
		{
			Init();

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
			Init();

			if (index < 0)
			{
				return SetCurrent(-1, null);
			}
			else
			{
				return index < _target.Value.Count && SetCurrent(index, _target.Value[index]);
			}
		}

		public bool MoveCurrentToFirst() => MoveCurrentToPosition(0); // No needs to Init: are not using any Current***

		public bool MoveCurrentToLast() => MoveCurrentToPosition(_target.Value.Count - 1); // No needs to Init: are not using any Current***

		public bool MoveCurrentToNext()
		{
			Init();
			return CurrentPosition + 1 < _target.Value.Count && MoveCurrentToPosition(CurrentPosition + 1);
		}

		public bool MoveCurrentToPrevious()
		{
			Init();
			return CurrentPosition > 0 && MoveCurrentToPosition(CurrentPosition - 1);
		}

		private bool SetCurrent(int index, object? value, bool isCancelable = true)
		{
			if (CurrentPosition == index && EqualityComparer<object?>.Default.Equals(CurrentItem, value))
			{
				// Current is already up to date, do not raise events for nothing!
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

			_service?.SelectFromView(index);

			_currentChanged.InvocationList?.Invoke(this, CurrentItem);

			return true;
		}

		public void Dispose()
		{
			if (_service is not null)
			{
				_service.StateChanged -= OnServiceStateChanged;
			}
		}
	}
}
