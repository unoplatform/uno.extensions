using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data;

/// <summary>
/// Class responsible to:
///		1. Detect changes compared to the previous collection (on bg thread)
///		2. Buffer the changes that occurs on collection (on Context thread)
///		3. Replay all the changes from delta (1.) and buffer (2.) (on Context thread)
///		4. Forward future changes that occurs on collection (on Context thread) to the target CollectionChangedFacet
/// </summary>
internal class DataLayerChangesBuffer : IDisposable
{
	private static class State
	{
		public const int New = 0;

		/// <summary>
		/// The buffer is active: It has detected changes between the previous collection and the updated one,
		/// and subscribe to collection changed on the new collection and append them to the buffer.
		/// We are waiting to reach the UI thread to replay all those changes (diff + collection changed event).
		/// </summary>
		public const int Initialized = 1;

		/// <summary>
		/// The diff changes has been forwarded to the _layer.Items.
		/// Now as soon as we receive a collection changed, we request to go to the UI thread to apply it on the _layer.Items.
		/// </summary>
		public const int Running = 2;

		/// <summary>
		/// We have unsubscribed from the collection changed event (so the _layer.Items collection is no longer updated).
		/// We can still un-buffer some changes TODO UNO
		/// </summary>
		public const int Stopped = 3;
		public const int Disposed = 4;
	}		    

	private int _state = State.New;
	private readonly SingleAssignmentDisposable _subscription = new();

	private DataLayerChangesBuffer? _previous; // The previous version
	private readonly ILayerHolder _layer;
	private readonly CollectionChangeSet? _changes;
	private readonly ILayerTracker _changesTracker;

	private ImmutableList<CollectionUpdater> _buffer = ImmutableList<CollectionUpdater>.Empty; // The changes raised while we are changing the thread to applied this version 

	private IObservableCollectionSnapshot? _stopItems; // the state of the collection when this tracker was stopped

	/// <summary>
	/// Gets the observe source collection handle by this changes manager
	/// </summary>
	public IObservableCollection Collection { get; }

	public DataLayerChangesBuffer(ILayerHolder layer, IObservableCollection source, ILayerTracker changesTracker)
		: this(null, layer, source, null, changesTracker)
	{
	}

	private DataLayerChangesBuffer(DataLayerChangesBuffer? previous, ILayerHolder layer, IObservableCollection source, CollectionChangeSet? changes, ILayerTracker changesTracker)
	{
		_previous = previous;
		_layer = layer;
		_changes = changes;
		_changesTracker = changesTracker;

		Collection = source;
	}

	public DataLayerChangesBuffer UpdateTo(IObservableCollection collection, CollectionChangeSet? changes)
		=> new(this, _layer, collection, changes, _changesTracker);

	public DataLayerUpdate Initialize(ILayerTracker tracker)
	{
		if (Interlocked.CompareExchange(ref _state, State.Initialized, State.New) != State.New)
		{
			throw new InvalidOperationException($"The buffer is not in the expected state (expected: 'New', actual: {_state})");
		}

		IObservableCollectionSnapshot? oldItems, newItems;
		if (_previous is null)
		{
			oldItems = null;

			// This is the initial tracker, we don't have to detect changes, we only have to propagate collection changes
			_subscription.Disposable = Collection.AddCollectionChangedHandler(Buffer, out newItems);
		}
		else
		{
			oldItems = _previous.Stop(); // Stop change propagation on previous collection
			_previous = null; // prevent leak

			// Init the buffer and then start collection change buffering / propagation
			_subscription.Disposable = Collection.AddCollectionChangedHandler(Buffer, out newItems);
		}

		var updater = tracker.GetChanges(oldItems, newItems, _changes, _layer.Items.HasListener);

		return new DataLayerUpdate(oldItems, updater, newItems, _layer.Items, tracker.Context, this);
	}

	/// <summary>
	/// Flush the current buffer and enable auto flushing for sub-sequent collection changed events.
	/// </summary>
	/// <remarks>This has to be invoked on the context/UI thread</remarks>
	/// <exception cref="InvalidOperationException">The buffer is in an invalid state.</exception>
	public void Start()
	{
		var state = Interlocked.CompareExchange(ref _state, State.Running, State.Initialized);
		if (state != State.Initialized && state != State.Stopped)
		{
			throw new InvalidOperationException($"The buffer is not in the expected state (expected: 'Initialized', actual: {_state})");
		}

		// Even if this buffer was already stopped (cf. Stop), we must flush the buffer to ensure to sync the view.
		Flush();
	}

	/// <summary>
	/// Disable collection changes tracking to freeze the collection in its current state.
	/// </summary>
	/// <remarks>This is expected to be invoked on a BG thread.</remarks>
	/// <returns>A snapshot of the final collection.</returns>
	/// <exception cref="InvalidOperationException"></exception>
	public IObservableCollectionSnapshot Stop()
	{
		// If two changes are made really quickly, this buffer may have not been started before being stopped by the next one.
		// If so (we will still be in 'Initialized' state) we accept the stop request and then handle it properly in the 'Start'.

		// As state cannot go back, it valid to try to change it twice without 'lock'
		if (Interlocked.CompareExchange(ref _state, State.Stopped, State.Initialized) is State.Initialized
			|| Interlocked.CompareExchange(ref _state, State.Stopped, State.Running) is State.Running)
		{
			Collection.RemoveCollectionChangedHandler(Buffer, out _stopItems);
			_subscription.Dispose();
		}
		else if (_stopItems is null)
		{
			// if _finalItems is null, we are in a non recoverable state so throw an explicit exception 
			// (which will prevent the update of the collection and avoid crash of the application)
			throw new InvalidOperationException($"The manager is not in the expected state (expected: 'Started', actual: {_state})");
		}
		else
		{
			this.Log().Warn($"The buffer is not in the expected state (expected: 'Started', actual: {_state}). " +
				"But as the final items are available, we can safely continue. " +
				"This may occurs if the collection is updated twice really quickly.");
		}

		return _stopItems!;
	}

	private void Buffer(RichNotifyCollectionChangedEventArgs arg)
	{
		// Note: even for 'Reset' we properly detect changes (instead of clearing the _buffer). 
		//		 This is to avoid invalid state due to ui callbacks not being invoked.

		var changes = _changesTracker.GetChanges(arg);
		ImmutableInterlocked.Update(ref _buffer, (buffer, c) => buffer.Add(c), changes);

		// Wait for initialization completion before auto flush
		if (_state == State.Running)
		{
			_layer.Schedule(Flush);
		}
	}

	private void Flush()
	{
		// TODO Uno: should we add this ??? -- WARNING We should probably still allow Start method!
		//if (_state is not State.Initialized and not State.Running)
		//{
		//	return;
		//}

		if (_buffer.Count > 0)
		{
			var buffer = Interlocked.Exchange(ref _buffer, ImmutableList<CollectionUpdater>.Empty);
			foreach (var changes in buffer)
			{
				_layer.Items.Update(changes);
			}
		}
	}

	public void Dispose()
	{
		_state = State.Disposed;
		_subscription.Dispose();
	}
}
