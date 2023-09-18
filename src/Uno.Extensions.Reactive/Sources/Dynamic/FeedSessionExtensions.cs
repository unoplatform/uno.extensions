using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive.Sources;

internal static class FeedSessionExtensions
{
	/// <summary>
	/// Gets or create an object that will be shared across all executions of the current session.
	/// </summary>
	/// <typeparam name="TKey">Type of the key</typeparam>
	/// <typeparam name="TValue">Type of the shared object</typeparam>
	/// <param name="session">The session to configure.</param>
	/// <param name="key">The key that identifies the value. It has to be unique between all dependencies.</param>
	/// <param name="factory">Factory to create the object if missing.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TValue GetShared<TKey, TValue>(this FeedSession session, TKey key, Func<FeedSession, TKey, TValue> factory)
		where TKey : notnull
		where TValue : notnull
		=> session.GetShared(key, static (sess, k, f) => f(sess, k), factory);

	/// <summary>
	/// Gets or create an object that will be shared across all executions of the current session.
	/// </summary>
	/// <typeparam name="TValue">Type of the shared object</typeparam>
	/// <param name="session">The session to configure.</param>
	/// <param name="factory">Factory to create the object if missing.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TValue GetShared<TValue>(this FeedSession session, Func<FeedSession, TValue> factory)
		where TValue : notnull
		=> session.GetShared(typeof(TValue), static (sess, _, f) => f(sess), factory);

	/// <summary>
	/// Enables the refresh **for the whole session**.
	/// </summary>
	/// <param name="session">The session to configure.</param>
	public static void EnableRefresh(this FeedSession session)
		=> session.GetShared(static sess => new RefreshDependency(sess)).Enable();

	/// <summary>
	/// Enables the refresh **for the whole session** using an external refresh signal.
	/// </summary>
	/// <param name="session">The session to configure.</param>
	/// <param name="signal">The external refresh signal.</param>
	public static void EnableRefresh(this FeedSession session, ISignal signal)
		=> session.GetShared((nameof(RefreshSignalDependency), signal), static (sess, _, sig) => new RefreshSignalDependency(sess, sig), signal);

	/// <summary>
	/// Enables the refresh **for the whole session** when the given  hot-reload.
	/// </summary>
	/// <param name="session"></param>
	public static void EnableHotReload(this FeedSession session)
		=> session.GetShared(static sess => new HotReloadDependency(sess));
}
