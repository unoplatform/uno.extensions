using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

/// <summary>
/// A queue of collection changes
/// </summary>
[DebuggerTypeProxy(typeof(DebuggerProxy))]
public sealed partial class CollectionChangesQueue
{
	public static CollectionChangesQueue Empty { get; } = new(new Node());

	private readonly Node _head;
	private readonly bool _isReset;
	private readonly IList? _resetOldItems, _resetNewItems;

	internal CollectionChangesQueue(RichNotifyCollectionChangedEventArgs change) 
		=> _head = new Node(change);

	internal CollectionChangesQueue(Node head) 
		=> _head = head;

	private CollectionChangesQueue(Node head, IList? oldItems, IList? newItems)
	{
		_head = head;
		_isReset = true;
		_resetOldItems = oldItems ?? Array.Empty<object>();
		_resetNewItems = newItems ?? Array.Empty<object>();
	}

	/// <summary>
	/// Convert this queue of changes to a new queue which contains only one reset event
	/// </summary>
	public CollectionChangesQueue ToReset(IList oldItems, IList newItems) 
		=> new(_head, oldItems, newItems);

	/// <summary>
	/// Dequeue a set of changes.
	/// </summary>
	/// <remarks>As this method may invoke some callbacks, it is intended to be use from the thread on which the final collection is maintained.</remarks>
	/// <param name="handler">An handler to handle the collection changes (this won't be invoke for callbacks).</param>
	/// <param name="silently">Determines if events should be raised on the handler or not (in that case, node's call backs are still going to be invoked).</param>
	public void DequeueChanges(IHandler? handler, bool silently = false)
	{
		if (_isReset)
		{
			_head.RunBeforeCallbacks();

			if (!silently)
			{
				handler?.Raise(RichNotifyCollectionChangedEventArgs.Reset(_resetOldItems, _resetNewItems));
			}

			_head.RunAfterCallbacks();
		}
		else if (handler is not null)
		{
			_head.ApplyTo(handler, silently: silently);
		}
	}
}
