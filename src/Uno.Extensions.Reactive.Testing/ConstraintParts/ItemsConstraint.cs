using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;

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
		var actualItemsOpt = (Option<IImmutableList<T>>)entry.Data;
		actualItemsOpt.Type.Should().Be(_items.Type);

		if (actualItemsOpt.IsSome(out var actualItems))
		{
			actualItems.Should().BeEquivalentTo(_items.SomeOrDefault()!);
		}
	}

	public static implicit operator ItemsConstraint<T>(Option<IImmutableList<T>> items)
		=> new(items);

	public static implicit operator ItemsConstraint<T>(T[] items)
		=> Items.Some(items);
}
