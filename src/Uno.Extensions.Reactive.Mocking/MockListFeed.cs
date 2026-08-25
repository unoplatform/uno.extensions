using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Reactive.Mocking;

/// <summary>
/// Typed vocabulary to build pinned list-feed states for mocking (spec 013 §4.1).
/// </summary>
public static class MockListFeed
{
	/// <summary>A list-feed pinned to the given items (Some).</summary>
	public static IListFeed<T> Value<T>(params T[] items)
		where T : notnull
		=> ListFeed.Async<T>(async _ => items.ToImmutableList());

	/// <summary>A list-feed pinned to None (no value).</summary>
	public static IListFeed<T> Empty<T>()
		where T : notnull
		=> ListFeed<T>.Async(async _ => Option<IImmutableList<T>>.None());

	/// <summary>A list-feed pinned to Some(empty list).</summary>
	public static IListFeed<T> EmptyList<T>()
		where T : notnull
		=> ListFeed.Async<T>(async _ => ImmutableList<T>.Empty);

	/// <summary>A list-feed pinned to an error.</summary>
	public static IListFeed<T> Error<T>(Exception error)
		where T : notnull
	{
		AsyncFunc<IImmutableList<T>> provider = async _ => throw error;
		return ListFeed.Async<T>(provider);
	}
}
