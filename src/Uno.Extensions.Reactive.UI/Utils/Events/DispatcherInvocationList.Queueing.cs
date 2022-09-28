using System;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Events;

internal class QueueingDispatcherInvocationList<THandler, TArgs> : DispatcherInvocationList<THandler, TArgs>
	where TArgs : class
{
	private Node _head, _tail;

	public QueueingDispatcherInvocationList(object owner, Func<THandler, Action<object, TArgs>> raiseMethod, IDispatcherInternal dispatcher)
		: base(owner, raiseMethod, dispatcher)
	{
		_head = _tail = new();
	}

	/// <inheritdoc />
	protected override void Enqueue(TArgs args)
	{
		var tail = new Node { Value = args };
		Interlocked.Exchange(ref _tail, tail).Next = tail;
	}

	/// <inheritdoc />
	protected override void Dequeue()
	{
		var tail = _tail;
		var node = Interlocked.Exchange(ref _head, tail);
		while (node != tail // The 'node' is the one we set as '_tail', so we should stop here (we will recurse 'Dequeue()' if tail has been updated anyway)
			&& (node = node?.Next) is not null) // The first node has already been raise or is the empty initial node
		{
			var args = node.Value;
			if (!RaiseCore(args))
			{
				return;
			}
		}

		if (_tail != tail)
		{
			// The '_tail' has been updated since we started to 'Dequeue',
			// recurse to complete the dequeuing while we have access to the dispatcher.
			Dequeue();
		}
	}

	private class Node
	{
		public TArgs? Value;

		public Node? Next;
	}
}
