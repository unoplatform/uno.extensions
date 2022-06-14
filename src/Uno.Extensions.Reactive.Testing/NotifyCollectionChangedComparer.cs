using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Uno.Extensions.Collections;

namespace Uno.Extensions.Reactive.Testing;

public class NotifyCollectionChangedComparer : IComparer, IEqualityComparer, IEqualityComparer<NotifyCollectionChangedEventArgs>
{
	private readonly IEqualityComparer<object> _itemComparer;
	public static NotifyCollectionChangedComparer Default { get; } = new NotifyCollectionChangedComparer(EqualityComparer<object>.Default);

	public NotifyCollectionChangedComparer(IEqualityComparer<object> itemComparer)
	{
		_itemComparer = itemComparer;
	}

	public int Compare(object? l, object? r)
		=> Equals(l, r)
			? 0
			: -1;

	public bool Equals(NotifyCollectionChangedEventArgs? left, NotifyCollectionChangedEventArgs? right)
	{
		if (left == null)
		{
			return right == null;
		}

		return right != null
			&& left.Action == right.Action
			&& left.OldStartingIndex == right.OldStartingIndex
			&& AreSequenceEquals(left.OldItems, right.OldItems)
			&& left.NewStartingIndex == right.NewStartingIndex
			&& AreSequenceEquals(left.NewItems, right.NewItems)
			&& (
				!(left is RichNotifyCollectionChangedEventArgs richLeft)
				|| !(right is RichNotifyCollectionChangedEventArgs richRight)
				|| (
					AreSequenceEquals(richLeft.ResetOldItems, richRight.ResetOldItems)
					&& AreSequenceEquals(richLeft.ResetNewItems, richRight.ResetNewItems)
				)
			);
	}

	private bool AreSequenceEquals(IList? left, IList? right)
		=> left?.Cast<object>().SequenceEqual(right?.Cast<object>() ?? Enumerable.Empty<object>(), _itemComparer) ?? right == null;

	public int GetHashCode(NotifyCollectionChangedEventArgs arg) => arg.Action.GetHashCode();

	public new bool Equals(object? l, object? r)
		=> l is NotifyCollectionChangedEventArgs left
			&& r is NotifyCollectionChangedEventArgs right
			&& Equals(left, right);

	public int GetHashCode(object obj) => obj is NotifyCollectionChangedEventArgs arg ? arg.Action.GetHashCode() : -1;
}
