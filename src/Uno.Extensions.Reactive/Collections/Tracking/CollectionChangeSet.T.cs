using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Collections.Tracking;

internal sealed partial record CollectionChangeSet<T> : CollectionChangeSet
{
	private readonly CollectionAnalyzer.Change<T>? _head;

	public static CollectionChangeSet<T> Empty { get; } = new(head: default);

	internal CollectionChangeSet(CollectionAnalyzer.Change<T>? head)
	{
		_head = head;
	}

	protected override IEnumerable<RichNotifyCollectionChangedEventArgs> Enumerate()
	{
		var node = _head;
		while (node is not null)
		{
			var args = node.ToEvent();
			if (args is not null)
			{
				yield return args;
			}

			node = node.Next;
		}
	}

	public override IEnumerator<IChange> GetEnumerator()
	{
		var node = _head;
		while (node is not null)
		{
			yield return node;

			node = node.Next;
		}
	}

	public override CollectionUpdater ToUpdater(ICollectionUpdaterVisitor visitor)
		=> _head is null
			? CollectionUpdater.Empty
			: new(_head.ToUpdater(visitor));

	/// <summary>
	/// Visits this change set.
	/// </summary>
	/// <param name="visitor">The visitor.</param>
	/// <remarks>
	/// This is almost equivalent to <see cref="Enumerate"/> and interpret each event args,
	/// but in a lighter way since it avoid the creation of all event args.
	/// </remarks>
	internal void Visit(ICollectionChangeSetVisitor<T> visitor)
	{
		var node = _head;
		while (node is not null)
		{
			node.Visit(visitor);

			node = node.Next;
		}
	}
}
