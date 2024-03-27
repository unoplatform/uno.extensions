using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Reactive.Testing;

public sealed class ItemsConstraint<T> : AxisConstraint
{
	private readonly Option<IImmutableList<T>> _items;

	public ItemsConstraint(IEnumerable<T> items)
	{
		_items = items.ToImmutableList() is { Count: > 0 } list
			? Option.Some(list as IImmutableList<T>)
			: Option<IImmutableList<T>>.None();
	}

	public ItemsConstraint(Option<IImmutableList<T>> items)
	{
		_items = items;
	}

	/// <inheritdoc />
	public override MessageAxis ConstrainedAxis => MessageAxis.Data;

	/// <inheritdoc />
	public override void Assert(IMessageEntry entry)
	{
		var actualItemsOpt = entry.Data;
		actualItemsOpt.Type.Should().Be(_items.Type);

		if (actualItemsOpt.IsSome(out var actualItems))
		{
			actualItems.Should().BeAssignableTo<IImmutableList<T>>();

			var actualImmutableItems = (IImmutableList<T>)actualItems;
			var expectedImmutableItems = _items.SomeOrDefault()!;

			actualImmutableItems.Count.Should().Be(expectedImmutableItems.Count);

			for (var i = 0; i < expectedImmutableItems.Count; i++)
			{
				using (AssertionScope.Current.ForContext($"item [{i}]"))
				{
					actualImmutableItems[i].Should().BeEquivalentTo(expectedImmutableItems[i]);
				}
			}
		}
	}

	public static implicit operator ItemsConstraint<T>(Option<IImmutableList<T>> items)
		=> new(items);

	public static implicit operator ItemsConstraint<T>(T[] items)
		=> Items.Some(items);
}
