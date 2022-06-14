using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;

namespace Uno.Extensions.Reactive.Testing;

public sealed class ItemsChanged<T> : ChangesConstraint
{
	private readonly RichNotifyCollectionChangedEventArgs[] _expectedArgs;

	public static ItemsChanged<T> Add(int index, params T[] items)
		=> new(RichNotifyCollectionChangedEventArgs.AddSome(items, index));

	public static ItemsChanged<T> Add(int index, IEnumerable<T> items)
		=> new(RichNotifyCollectionChangedEventArgs.AddSome(items.ToList(), index));

	public static ItemsChanged<T> Remove(int index, params T[] items)
		=> new(RichNotifyCollectionChangedEventArgs.RemoveSome(items, index));

	public static ItemsChanged<T> Remove(int index, IEnumerable<T> items)
		=> new(RichNotifyCollectionChangedEventArgs.RemoveSome(items.ToList(), index));

	public static ItemsChanged<T> Replace(int index, IEnumerable<T> oldItems, IEnumerable<T> newItems)
		=> new(RichNotifyCollectionChangedEventArgs.ReplaceSome(oldItems.ToList(), newItems.ToList(), index));

	public static ItemsChanged<T> Move(int oldIndex, int newIndex, params T[] items)
		=> new(RichNotifyCollectionChangedEventArgs.MoveSome(items.ToList(), oldIndex, newIndex));

	public static ItemsChanged<T> Move(int oldIndex, int newIndex, IEnumerable<T> items)
		=> new(RichNotifyCollectionChangedEventArgs.MoveSome(items.ToList(), oldIndex, newIndex));

	public static ItemsChanged<T> Reset(IEnumerable<T> oldItems, IEnumerable<T> newItems)
		=> new(RichNotifyCollectionChangedEventArgs.Reset(oldItems.ToList(), newItems.ToList()));

	public static ItemsChanged<T> Reset(IEnumerable<T> newItems)
		=> new(RichNotifyCollectionChangedEventArgs.Reset(null, newItems.ToList()));

	internal ItemsChanged(params RichNotifyCollectionChangedEventArgs[] expectedArgs)
		=> _expectedArgs = expectedArgs;

	/// <inheritdoc />
	public override void Assert(ChangeCollection actual)
	{
		actual.Contains(MessageAxis.Data, out var changeSet).Should().BeTrue();
		changeSet.Should().BeOfType<CollectionChangeSet>();

		if (changeSet is CollectionChangeSet changes)
		{
			var actualArgs = changes.ToCollectionChanges().ToList();

			actualArgs.Count.Should().Be(_expectedArgs.Length);

			for (var i = 0; i < Math.Min(actualArgs.Count, _expectedArgs.Length); i++)
			{
				var actualArg = actualArgs[i];
				var expectedArg = _expectedArgs[i];

				NotifyCollectionChangedComparer.Default.Equals(actualArg, expectedArg).Should().BeTrue($"arg [{i}] should be the same as expected");
			}
		}
	}
}
