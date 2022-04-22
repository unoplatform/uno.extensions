using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Collections.Tracking;

internal sealed partial record CollectionChangeSet : IChangeSet
{
	private readonly CollectionAnalyzer.Change? _head;

	public static CollectionChangeSet Empty { get; } = new(head: default);

	internal CollectionChangeSet(CollectionAnalyzer.Change? head)
	{
		_head = head;
	}

	internal ICollection<RichNotifyCollectionChangedEventArgs> ToCollectionChanges()
		=> Enumerate().ToList();

	private IEnumerable<RichNotifyCollectionChangedEventArgs> Enumerate()
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

	/// <inheritdoc />
	public IEnumerator<IChange> GetEnumerator()
	{
		var node = _head;
		while (node is not null)
		{
			yield return node;

			node = node.Next;
		}
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	public CollectionUpdater ToUpdater(ICollectionUpdaterVisitor visitor)
		=> _head is null
			? CollectionUpdater.Empty
			: new(_head.ToUpdater(visitor));
}
