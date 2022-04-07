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
	private readonly INode? _head;
	private readonly bool _isReset;
	private readonly IList? _resetOldItems, _resetNewItems;

	internal CollectionChangesQueue(RichNotifyCollectionChangedEventArgs change) 
		=> _head = new ArgsToNodeAdapter(change);

	internal CollectionChangesQueue(INode? head) 
		=> _head = head;

	private CollectionChangesQueue(INode? head, IList? oldItems, IList? newItems)
	{
		_head = head;
		_isReset = true;
		_resetOldItems = oldItems ?? Array.Empty<object>();
		_resetNewItems = newItems ?? Array.Empty<object>();
	}

	private IEnumerable<INode> GetNodes()
	{
		var node = _head;
		while (node != null)
		{
			yield return node;
			node = node.Next;
		}
	}

	/// <summary>
	/// Convert the collection of collection changes into a collection of <see cref="NotifyCollectionChangedEventArgs"/>.
	/// </summary>
	/// <remarks>This DOES NOT check types! You MUST NOT use this method if you detected changes using a <see cref="ICollectionTrackingVisitor"/>.</remarks>
	internal ICollection<NotifyCollectionChangedEventArgs> ToCollectionChanges()
	{
		return GetNodes()
			.Select(node => node.ToEvent() as NotifyCollectionChangedEventArgs)
			.ToList();
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
			foreach (var node in GetNodes())
			{
				node.RunBeforeCallbacks();
			}

			if (!silently)
			{
				handler?.Raise(RichNotifyCollectionChangedEventArgs.Reset(_resetOldItems, _resetNewItems));
			}

			foreach (var node in GetNodes())
			{
				node.RunAfterCallbacks();
			}
		}
		else if (handler is not null)
		{
			foreach (var node in GetNodes())
			{
				node.ApplyTo(handler, silently: silently);
			}
		}
	}

	/// <inheritdoc />
	public override string ToString()
		=> string.Join(Environment.NewLine, GetNodes());
}
