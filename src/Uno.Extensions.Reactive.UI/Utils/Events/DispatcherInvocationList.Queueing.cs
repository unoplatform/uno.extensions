using System;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive.Events;

internal class QueueingDispatcherInvocationList<THandler, TArgs> : DispatcherInvocationList<THandler, TArgs>
	where TArgs : class
{
	private Node _head, _tail;

	public QueueingDispatcherInvocationList(object owner, Func<THandler, Action<object, TArgs>> raiseMethod, DispatcherQueue dispatcher)
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
		var node = Interlocked.Exchange(ref _head, _tail);
		while ((node = node?.Next) is not null) // The first node has already been raise or is the empty initial node
		{
			var args = node.Value;
			if (!RaiseCore(args))
			{
				return;
			}
		}
	}

	private class Node
	{
		public TArgs? Value;

		public Node? Next;
	}
}
