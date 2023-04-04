using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;

namespace Uno.Extensions.Reactive.Testing;

public sealed class ItemsChanged : ChangesConstraint
{
	private readonly IImmutableList<RichNotifyCollectionChangedEventArgs> _expectedArgs;

	public static ItemsChanged Empty { get; } = new();

	public static ItemsChanged Add<T>(int index, params T[] items)
		=> new(RichNotifyCollectionChangedEventArgs.AddSome<T>(items, index));

	public static ItemsChanged Add<T>(int index, IEnumerable<T> items)
		=> new(RichNotifyCollectionChangedEventArgs.AddSome<T>(items.ToList(), index));

	public static ItemsChanged Remove<T>(int index, params T[] items)
		=> new(RichNotifyCollectionChangedEventArgs.RemoveSome<T>(items, index));

	public static ItemsChanged Remove<T>(int index, IEnumerable<T> items)
		=> new(RichNotifyCollectionChangedEventArgs.RemoveSome<T>(items.ToList(), index));

	public static ItemsChanged Replace<T>(int index, IEnumerable<T> oldItems, IEnumerable<T> newItems, bool isReplaceOfSameEntities)
		=> new(RichNotifyCollectionChangedEventArgs.ReplaceSome<T>(oldItems.ToList(), newItems.ToList(), index, isReplaceOfSameEntities));

	public static ItemsChanged Replace<T>(int index, T oldItem, T newItem, bool isReplaceOfSameEntity)
		=> new(RichNotifyCollectionChangedEventArgs.Replace<T>(oldItem, newItem, index, isReplaceOfSameEntity));

	public static ItemsChanged Move<T>(int oldIndex, int newIndex, params T[] items)
		=> new(RichNotifyCollectionChangedEventArgs.MoveSome<T>(items.ToList(), oldIndex, newIndex));

	public static ItemsChanged Move<T>(int oldIndex, int newIndex, IEnumerable<T> items)
		=> new(RichNotifyCollectionChangedEventArgs.MoveSome<T>(items.ToList(), oldIndex, newIndex));

	public static ItemsChanged Reset<T>(IEnumerable<T> oldItems, IEnumerable<T> newItems)
		=> new(RichNotifyCollectionChangedEventArgs.Reset<T>(oldItems.ToList(), newItems.ToList()));

	public static ItemsChanged Reset<T>(IEnumerable<T> newItems)
		=> new(RichNotifyCollectionChangedEventArgs.Reset<T>(null, newItems.ToList()));

	public static ItemsChanged operator &(ItemsChanged left, ItemsChanged right)
		=> new(left._expectedArgs.Concat(right._expectedArgs).ToImmutableList());

	internal ItemsChanged(params RichNotifyCollectionChangedEventArgs[] expectedArgs)
		=> _expectedArgs = expectedArgs.ToImmutableList();

	internal ItemsChanged(IImmutableList<RichNotifyCollectionChangedEventArgs> expectedArgs)
		=> _expectedArgs = expectedArgs.ToImmutableList();


	/// <inheritdoc />
	public override void Assert(ChangeCollection actual)
	{
		using var _ = AssertionScope.Current.ForContext("of items (Data)");

		if (!actual.Contains(MessageAxis.Data, out var changeSet))
		{
			AssertionScope.Current.Fail("is not set, but the collection of items was expected to have been updated.");
		}
		changeSet.Should().BeAssignableTo<CollectionChangeSet>();

		if (changeSet is CollectionChangeSet changes)
		{
			var actualArgs = changes.ToCollectionChanges().ToList();

			using (AssertionScope.Current.ForContext(", the number of collection changed events args"))
			{
				actualArgs.Count.Should().Be(_expectedArgs.Count);
			}

			for (var i = 0; i < Math.Min(actualArgs.Count, _expectedArgs.Count); i++)
			{
				var actualArg = actualArgs[i];
				var expectedArg = _expectedArgs[i];

				if (!NotifyCollectionChangedComparer.Default.Equals(actualArg, expectedArg))
				{
					AssertionScope.Current.Fail($", the collection changed event arg #{i} expected to be {expectedArg}, but found {actualArg}");
				}
			}
		}
	}
}
