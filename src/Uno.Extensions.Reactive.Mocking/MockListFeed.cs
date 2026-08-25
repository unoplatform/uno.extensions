using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Mocking;

/// <summary>
/// Typed vocabulary to build pinned list-feed states for mocking (spec 013 §4.1).
/// </summary>
public static class MockListFeed
{
	/// <summary>A list-feed pinned to the given items (Some).</summary>
	public static IListFeed<T> Value<T>(params T[] items) where T : notnull
		=> Pinned<T>(b => b.Data((IImmutableList<T>)items.ToImmutableList()));

	/// <summary>A list-feed pinned to None (no value).</summary>
	public static IListFeed<T> Empty<T>() where T : notnull
		=> Pinned<T>(b => b.Data(Option<IImmutableList<T>>.None()));

	/// <summary>A list-feed pinned to Some(empty list).</summary>
	public static IListFeed<T> EmptyList<T>() where T : notnull
		=> Pinned<T>(b => b.Data((IImmutableList<T>)ImmutableList<T>.Empty));

	/// <summary>A list-feed pinned to Undefined (pre-first-emission).</summary>
	public static IListFeed<T> Undefined<T>() where T : notnull
		=> Pinned<T>(b => b);

	/// <summary>A list-feed pinned to a transient/indeterminate loading state.</summary>
	public static IListFeed<T> Loading<T>() where T : notnull
		=> Pinned<T>(b => b.IsTransient(true));

	/// <summary>A list-feed pinned to an error.</summary>
	public static IListFeed<T> Error<T>(Exception error) where T : notnull
		=> Pinned<T>(b => b.Error(error));

	/// <summary>A list-feed pinned to a stale value with a transient progress (refreshing).</summary>
	public static IListFeed<T> Refreshing<T>(params T[] staleItems) where T : notnull
		=> Pinned<T>(b => b.Data((IImmutableList<T>)staleItems.ToImmutableList()).IsTransient(true));

	private static IListFeed<T> Pinned<T>(Func<MessageBuilder<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> configure)
		where T : notnull
	{
		Message<IImmutableList<T>> message = configure(Message<IImmutableList<T>>.Initial.With());
		return ListFeed.Create<T>(_ => Yield(message));
	}

	private static async IAsyncEnumerable<Message<IImmutableList<T>>> Yield<T>(Message<IImmutableList<T>> message)
	{
		yield return message;
		await Task.CompletedTask;
	}
}
