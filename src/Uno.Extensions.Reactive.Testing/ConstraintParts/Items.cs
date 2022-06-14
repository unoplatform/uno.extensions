using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions.Execution;

namespace Uno.Extensions.Reactive.Testing;

public static class Items
{
	public static ItemsConstraint<T> Some<T>(IEnumerable<T> items)
		=> new(items.ToImmutableList() is { Count: > 0 } list
			? Option.Some(list as IImmutableList<T>)
			: throw new InvalidOperationException("An empty list is expected to be treated as None."));

	public static ItemsConstraint<T> Some<T>(params T[] items)
		=> new(items.ToImmutableList() is { Count: > 0 } list
			? Option.Some(list as IImmutableList<T>)
			: throw new InvalidOperationException("An empty list is expected to be treated as None."));

	public static ItemsChanged<T> Add<T>(int at, params T[] items)
		=> ItemsChanged<T>.Add(at, items);

	public static ItemsChanged<T> Add<T>(int at, IEnumerable<T> items)
		=> ItemsChanged<T>.Add(at, items);

	public static ItemsChanged<T> Remove<T>(int at, params T[] items)
		=> ItemsChanged<T>.Remove(at, items);

	public static ItemsChanged<T> Remove<T>(int at, IEnumerable<T> items)
		=> ItemsChanged<T>.Remove(at, items);

	public static ItemsChanged<T> Replace<T>(int at, IEnumerable<T> oldItems, IEnumerable<T> newItems)
		=> ItemsChanged<T>.Replace(at, oldItems, newItems);

	public static ItemsChanged<T> Move<T>(int from, int to, params T[] items)
		=> ItemsChanged<T>.Move(from, to, items);

	public static ItemsChanged<T> Move<T>(int from, int to, IEnumerable<T> items)
		=> ItemsChanged<T>.Move(from, to, items);

	public static ItemsChanged<T> Reset<T>(IEnumerable<T> oldItems, IEnumerable<T> newItems)
		=> ItemsChanged<T>.Reset(oldItems, newItems);

	public static ItemsChanged<T> Reset<T>(IEnumerable<T> newItems)
		=> ItemsChanged<T>.Reset(newItems);
}
