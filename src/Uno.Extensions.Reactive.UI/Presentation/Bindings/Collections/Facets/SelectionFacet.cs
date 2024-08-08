#pragma warning disable Uno0001 // ISelectionInfo is only an interface!

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.Foundation.Collections;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Bindings.Collections.Services;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;

/// <summary>
/// The selection facet of the ICollectionView
/// </summary>
internal class SelectionFacet : IDisposable, ISelectionInfo
{
	/* 
	* Note: The selection is sync between the ListView and the CollectionView only when SelectionMode is 'Single'
	*		 
	*		 For modes 'Multiple' and 'Extended' the ListView notifies using the `ISelectionInfo` interface.
	*		 
	*		 When we cycle between the selection modes, only the selection made in 'Single' is kept 
	*		 (ie. the selection made in other modes is flushed when mode change, and the 'None' doesn't alter the Current on the collection View)
	*		 
	*		 When we unselect the 'CurrentItem', list view calls 'MoveCurrentTo(null)'.
	*/

	private readonly EventRegistrationTokenTable<CurrentChangedEventHandler> _currentChanged = new();
	private readonly EventRegistrationTokenTable<CurrentChangingEventHandler> _currentChanging = new();
	private readonly ISelectionService? _service;
	private readonly Lazy<IObservableVector<object>> _target;
	private readonly IDispatcher? _dispatcher;
	private readonly CollectionChangedFacet _collectionChangedFacet;

	private bool _isInit;

	public SelectionFacet(IBindableCollectionViewSource source, CollectionChangedFacet collectionChangedFacet, Func <IObservableVector<object>> target)
	{
		_service = source.GetService(typeof(ISelectionService)) as ISelectionService;
		_target = new Lazy<IObservableVector<object>>(target, LazyThreadSafetyMode.None);
		_dispatcher = source.Dispatcher;
		_collectionChangedFacet = collectionChangedFacet;

#if __WINDOWS__
		if (_service is not null)
		{
			UpdateLocalSelection(_service.GetSelectedRanges());
		}
#endif
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
		_collectionChangedFacet.AddCollectionChangedHandler(RestoreCurrentItemOnSourceChanged, lowPriority: true);
	}

	private void OnServiceStateChanged(object? snd, EventArgs args)
	{
		if (_service!.IsSelected(CurrentPosition))
		{
			return;
		}

		var ranges = _service.GetSelectedRanges();

#if __WINDOWS__
		UpdateLocalSelection(ranges);
#endif

		if (!_service.IsSelected(CurrentPosition))
		{
			var index = ranges.FirstOrDefault()?.FirstIndex ?? -1;
			if (_dispatcher is null or { HasThreadAccess: true })
			{
				MoveCurrentToPosition(index);
			}
			else
			{
				_dispatcher.TryEnqueue(() => MoveCurrentToPosition(index));
			}
		}
	}

	private void RestoreCurrentItemOnSourceChanged(object? sender, NotifyCollectionChangedEventArgs args)
	{
		// Note about the usage of the CurrentPosition here:
		//		The ListView won't sync the Current<Item|Position> is the source implements ISelectionRange
		//		BUT it will still listen to CurrentChanged event.
		//		We keep it in sync on our side (cf. SelectRange and DeselectRange), so if the SelectedItem is updated by the VM (Replace with IsReplaceOfSameEntities flag),
		//		we are be able to properly restore it here.
		// Note: This will work only for the first selected item, i.e. only for SelectionMode.Single which is the main case we are interested in (master-details).

		// Other actions (like Remove and Reset) will be pushed by the ListView itself.

		if (args is RichNotifyCollectionChangedEventArgs { Action: NotifyCollectionChangedAction.Replace, IsReplaceOfSameEntities: true }
			&& CurrentPosition >= args.OldStartingIndex
			&& CurrentPosition < args.OldStartingIndex + args.OldItems!.Count)
		{
			MoveCurrentToPosition(CurrentPosition, isCancelable: false, isSelectionRangeUpdate: true);
		}
	}

	#region ICollectionView.Current (Single selection mode)
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
		=> MoveCurrentToPosition(index, isCancelable: true, isSelectionRangeUpdate: false);

	private bool MoveCurrentToPosition(int index, bool isCancelable, bool isSelectionRangeUpdate)
	{
		Init();

		if (index < 0)
		{
			return SetCurrent(-1, null, isCancelable, isSelectionRangeUpdate);
		}
		else
		{
			return index < _target.Value.Count && SetCurrent(index, _target.Value[index], isCancelable, isSelectionRangeUpdate);
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

	private bool SetCurrent(int index, object? value, bool isCancelable = true, bool isSelectionRangeUpdate = false)
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

		var oldPosition = CurrentPosition;

		CurrentPosition = index;
		CurrentItem = value;

		if (!isSelectionRangeUpdate)
		{
			if (index >= 0)
			{
				SelectRange(new ItemIndexRange(index, 1));
			}
			else if (oldPosition >= 0)
			{
				// Note: we unselect only if the new position is -1 (and old was not -1), otherwise we would prevent multi-selection!
				DeselectRange(new ItemIndexRange(oldPosition, 1));
			}
		}

		_currentChanged.InvocationList?.Invoke(this, CurrentItem);

		return true;
	}
	#endregion

	#region ISelectionInfo (Multi selection modes)
	/// <inheritdoc />
	public void SelectRange(ItemIndexRange itemIndexRange)
	{
		if (_service is null)
		{
			return;
		}

#if __WINDOWS__
		Debug.Assert(itemIndexRange.Length is 1); // Required for local coercing
		_localSelection.Add(itemIndexRange);
#endif
		_service.SelectRange(itemIndexRange);

#if UAP10_0_19041
		if (_service.GetSelectedRanges() is { Count: 1 } ranges && ranges[0] is {Length: 1} singleSelection)
#else
		if (_service.GetSelectedRanges() is [{ Length: 1 } singleSelection])
#endif
		{
			MoveCurrentToPosition(singleSelection.FirstIndex, isCancelable: false, isSelectionRangeUpdate: true);
		}
	}

	/// <inheritdoc />
	public void DeselectRange(ItemIndexRange itemIndexRange)
	{
		if (_service is null)
		{
			return;
		}

#if __WINDOWS__
		_localSelection.Remove(itemIndexRange);
#endif
		_service.DeselectRange(itemIndexRange);

		if (CurrentPosition >= itemIndexRange.FirstIndex
			&& CurrentPosition <= itemIndexRange.LastIndex
#if UAP10_0_19041
			&& _service.GetSelectedRanges() is { Count: 1 } ranges && ranges[0] is { Length: 1 } singleSelection)
#else
			&& _service.GetSelectedRanges() is [{ Length: 1 } singleSelection])
#endif
		{
			MoveCurrentToPosition(singleSelection.FirstIndex, isCancelable: false, isSelectionRangeUpdate: true);
		}
	}

	/// <inheritdoc />
	public bool IsSelected(int index)
		=> _service?.IsSelected(index) ?? false;

#if __WINDOWS__
	// On Windows the ListView does not accept to coerce the ranges.
	// Instead we try to keep the range instances provided by the ListView.
	private readonly List<ItemIndexRange> _localSelection = new();

	/// <inheritdoc />
	public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
		=> _localSelection;

	private void UpdateLocalSelection(IReadOnlyList<ItemIndexRange> svcRanges)
	{
		// Here we make sure that 'ranges' we are receiving from the _service are the same as _localSelection
		// Note: We assume that the ListView on window creates only range of 1 item.

		if (_localSelection is { Count: 0 })
		{
			_localSelection.AddRange(svcRanges);
			return;
		}

		var localIndexes = _localSelection
			.SelectMany(r => Enumerable.Range(r.FirstIndex, (int)r.Length))
			.ToList();

		var serviceIndexes = svcRanges
			.SelectMany(r => Enumerable.Range(r.FirstIndex, (int)r.Length))
			.ToList();

		foreach (var index in serviceIndexes)
		{
			if (!localIndexes.Contains(index))
			{
				_localSelection.Add(new ItemIndexRange(index, 1));
			}
		}

		foreach (var index in localIndexes)
		{
			if (!serviceIndexes.Contains(index))
			{ 
				_localSelection.RemoveAll(r => r.FirstIndex == index);
			}
		}
	}
#else
	/// <inheritdoc />
	public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
		=> _service?.GetSelectedRanges() ?? Array.Empty<ItemIndexRange>();
#endif

#endregion

	public void Dispose()
	{
		if (_service is not null)
		{
			_service.StateChanged -= OnServiceStateChanged;
		}
		_collectionChangedFacet.RemoveCollectionChangedHandler(RestoreCurrentItemOnSourceChanged, lowPriority: true);
	}
}
