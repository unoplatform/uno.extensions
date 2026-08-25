using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Mocking;

/// <summary>
/// Typed vocabulary to build pinned scalar feed states for mocking (spec 013 §4.1). Strongly typed
/// end to end; never accepts the tier-1 <c>MessageEntry</c> or any untyped envelope (D9/NG7).
/// </summary>
public static class MockFeed
{
	/// <summary>A feed pinned to a value (Some).</summary>
	public static IFeed<T> Value<T>(T value) where T : notnull
		=> Pinned<T>(b => b.Data(value));

	/// <summary>A feed pinned to None (no value).</summary>
	public static IFeed<T> Empty<T>() where T : notnull
		=> Pinned<T>(b => b.Data(Option<T>.None()));

	/// <summary>A feed pinned to Undefined (pre-first-emission).</summary>
	public static IFeed<T> Undefined<T>() where T : notnull
		=> Pinned<T>(b => b);

	/// <summary>A feed pinned to a transient/indeterminate loading state (IsExecuting stays true).</summary>
	public static IFeed<T> Loading<T>() where T : notnull
		=> Pinned<T>(b => b.IsTransient(true));

	/// <summary>A feed pinned to an error.</summary>
	public static IFeed<T> Error<T>(Exception error) where T : notnull
		=> Pinned<T>(b => b.Error(error));

	/// <summary>A feed pinned to a stale value with a transient progress (refreshing).</summary>
	public static IFeed<T> Refreshing<T>(T staleValue) where T : notnull
		=> Pinned<T>(b => b.Data(staleValue).IsTransient(true));

	private static IFeed<T> Pinned<T>(Func<MessageBuilder<T>, MessageBuilder<T>> configure)
		where T : notnull
	{
		Message<T> message = configure(Message<T>.Initial.With());
		return Feed.Create<T>(_ => Yield(message));
	}

	private static async IAsyncEnumerable<Message<T>> Yield<T>(Message<T> message)
	{
		yield return message;
		await Task.CompletedTask;
	}
}
