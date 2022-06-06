using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Collections;

namespace Uno.Extensions.Reactive.Tests.Collections;

[TestClass]
public class Given_CollectionChanged
{
	[TestMethod]
	public void When_Replace()
	{
		var arg = CollectionChanged.Replace(0, 1, 0);

		CollectionAssert.AreEqual(new[] {0}, arg.OldItems);
		CollectionAssert.AreEqual(new[] {1}, arg.NewItems);
	}

	[TestMethod]
	public void When_ReplaceSome()
	{
		var oldItems = new[] { 0, 1 };
		var newItems = new[] { 2, 3 };
		var arg = CollectionChanged.ReplaceSome(oldItems, newItems, 0);

		CollectionAssert.AreEqual(oldItems, arg.OldItems);
		CollectionAssert.AreEqual(newItems, arg.NewItems);
	}

	[TestMethod]
	public void When_ItemPerItem_ReplaceSome()
	{
		var arg = CollectionChanged.ReplaceSome(new[] { 1, 2, 3, 4 }, new[] { 5, 6, 7, 8 }, 0);
		var perItem = new List<NotifyCollectionChangedEventArgs>();
		var correction = 0;

		CollectionChanged.RaiseItemPerItem(arg, perItem.Add, ref correction);

		var expected = new[]
		{
			CollectionChanged.Replace(1, 5, 0),
			CollectionChanged.Replace(2, 6, 1),
			CollectionChanged.Replace(3, 7, 2),
			CollectionChanged.Replace(4, 8, 3)
		};

		Assert.IsTrue(perItem.SequenceEqual(expected, NotifyCollectionChangedComparer.Instance));
	}

	[TestMethod]
	public void When_ItemPerItem_ReplaceSome_WithLessItems()
	{
		var arg = CollectionChanged.ReplaceSome(new[] {1, 2, 3, 4}, new[] {5, 6}, 0);
		var perItem = new List<NotifyCollectionChangedEventArgs>();
		var correction = 0;

		CollectionChanged.RaiseItemPerItem(arg, perItem.Add, ref correction);

		var expected = new[]
		{
			CollectionChanged.Replace(1, 5, 0),
			CollectionChanged.Replace(2, 6, 1),
			CollectionChanged.Remove(3, 2),
			CollectionChanged.Remove(4, 2)
		};

		Assert.IsTrue(perItem.SequenceEqual(expected, NotifyCollectionChangedComparer.Instance));
	}

	[TestMethod]
	public void When_ItemPerItem_ReplaceSome_WithMoreItems()
	{
		var arg = CollectionChanged.ReplaceSome(new[] { 1, 2 }, new[] { 3, 4, 5, 6 }, 0);
		var perItem = new List<NotifyCollectionChangedEventArgs>();
		var correction = 0;

		CollectionChanged.RaiseItemPerItem(arg, perItem.Add, ref correction);

		var expected = new[]
		{
			CollectionChanged.Replace(1, 3, 0),
			CollectionChanged.Replace(2, 4, 1),
			CollectionChanged.Add(5, 2),
			CollectionChanged.Add(6, 3)
		};

		Assert.IsTrue(perItem.SequenceEqual(expected, NotifyCollectionChangedComparer.Instance));
	}

	internal class NotifyCollectionChangedComparer : IComparer, IEqualityComparer, IEqualityComparer<NotifyCollectionChangedEventArgs>
	{
		public static NotifyCollectionChangedComparer Instance { get; } = new();

		static NotifyCollectionChangedComparer() { }

		public int Compare(object? l, object? r)
			=> Equals(l, r)
				? 0
				: -1;

		public bool Equals(NotifyCollectionChangedEventArgs? left, NotifyCollectionChangedEventArgs? right)
			=> left!.Action == right!.Action
				&& left.OldStartingIndex == right.OldStartingIndex
				&& (left.OldItems?.Cast<object>().SequenceEqual(right.OldItems!.Cast<object>(), EqualityComparer<object>.Default) ?? right.OldItems == null)
				&& left.NewStartingIndex == right.NewStartingIndex
				&& (left.NewItems?.Cast<object>().SequenceEqual(right.NewItems!.Cast<object>(), EqualityComparer<object>.Default) ?? right.NewItems == null);

		public int GetHashCode(NotifyCollectionChangedEventArgs arg) => arg.Action.GetHashCode();

		public new bool Equals(object? l, object? r) => l is NotifyCollectionChangedEventArgs left
			&& r is NotifyCollectionChangedEventArgs right
			&& Equals(left, right);

		public int GetHashCode(object obj) => obj is NotifyCollectionChangedEventArgs arg ? arg.Action.GetHashCode() : -1;
	}
}
