using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions.Execution;

namespace Uno.Extensions.Reactive.Testing;

public static class Items
{
	public static ItemsConstraint<int> Range(uint count)
		=> new(count is 0
			? Option.None<IImmutableList<int>>()
			: Option.Some(Enumerable.Range(0, (int)count).ToImmutableList() as IImmutableList<int>));

	public static ItemsConstraint<T> Some<T>(IEnumerable<T> items)
		=> new(items.ToImmutableList() is { Count: > 0 } list
			? Option.Some(list as IImmutableList<T>)
			: throw new InvalidOperationException("An empty list is expected to be treated as None."));

	public static ItemsConstraint<T> Some<T>(params T[] items)
		=> new(items.ToImmutableList() is { Count: > 0 } list
			? Option.Some(list as IImmutableList<T>)
			: throw new InvalidOperationException("An empty list is expected to be treated as None."));

	public static ItemsChanged Add<T>(int at, params T[] items)
		=> ItemsChanged.Add(at, items);

	public static ItemsChanged Add<T>(int at, IEnumerable<T> items)
		=> ItemsChanged.Add(at, items);

	public static ItemsChanged Remove<T>(int at, params T[] items)
		=> ItemsChanged.Remove(at, items);

	public static ItemsChanged Remove<T>(int at, IEnumerable<T> items)
		=> ItemsChanged.Remove(at, items);

	public static ItemsChanged Replace<T>(int at, IEnumerable<T> oldItems, IEnumerable<T> newItems)
		=> ItemsChanged.Replace(at, oldItems, newItems);

	public static ItemsChanged Move<T>(int from, int to, params T[] items)
		=> ItemsChanged.Move(from, to, items);

	public static ItemsChanged Move<T>(int from, int to, IEnumerable<T> items)
		=> ItemsChanged.Move(from, to, items);

	public static ItemsChanged Reset<T>(IEnumerable<T> oldItems, IEnumerable<T> newItems)
		=> ItemsChanged.Reset(oldItems, newItems);

	public static ItemsChanged Reset<T>(IEnumerable<T> newItems)
		=> ItemsChanged.Reset(newItems);

	public static ItemsChanged NotChanged { get; } = ItemsChanged.Empty;
}
