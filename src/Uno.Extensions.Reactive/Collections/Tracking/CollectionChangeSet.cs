using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Collections.Tracking;

internal abstract partial record CollectionChangeSet : IChangeSet
{
	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	/// <inheritdoc />
	public abstract IEnumerator<IChange> GetEnumerator();

	internal ICollection<RichNotifyCollectionChangedEventArgs> ToCollectionChanges()
		=> Enumerate().ToList();

	protected abstract IEnumerable<RichNotifyCollectionChangedEventArgs> Enumerate();

	public abstract CollectionUpdater ToUpdater(ICollectionUpdaterVisitor visitor);
}
