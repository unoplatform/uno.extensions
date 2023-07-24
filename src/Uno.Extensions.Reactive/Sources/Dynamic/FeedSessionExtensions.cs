using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

internal static class FeedSessionExtensions
{
	/// <summary>
	/// Enables the refresh **for the whole session**.
	/// </summary>
	/// <param name="session">The session to configure.</param>
	public static void EnableRefresh(this FeedSession session)
		=> session.GetShared(nameof(RefreshDependency), static (sess, _, __) => new RefreshDependency(sess), Unit.Default).Enable();

	/// <summary>
	/// Enables the refresh **for the whole session** using an external refresh signal.
	/// </summary>
	/// <param name="session">The session to configure.</param>
	/// <param name="signal">The external refresh signal.</param>
	public static void EnableRefresh(this FeedSession session, ISignal signal)
		=> session.GetShared((nameof(RefreshSignalDependency), signal), static (sess, _, sig) => new RefreshSignalDependency(sess, sig), signal);
}
