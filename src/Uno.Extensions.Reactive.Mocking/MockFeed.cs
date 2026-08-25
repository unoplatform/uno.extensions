using System;

namespace Uno.Extensions.Reactive.Mocking;

/// <summary>
/// Typed vocabulary to build pinned scalar feed states for mocking (spec 013 §4.1). Strongly typed
/// end to end; never accepts the tier-1 <c>MessageEntry</c> or any untyped envelope (D9/NG7).
/// </summary>
public static class MockFeed
{
	/// <summary>A feed pinned to a value (Some).</summary>
	public static IFeed<T> Value<T>(T value)
		where T : notnull
		=> Feed.Async(async _ => value);

	/// <summary>A feed pinned to an error.</summary>
	public static IFeed<T> Error<T>(Exception error)
		where T : notnull
		=> Feed.Async<T>(async _ => throw error);
}
