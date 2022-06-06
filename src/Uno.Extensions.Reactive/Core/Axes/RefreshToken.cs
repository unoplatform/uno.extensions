using System;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Core;
using static Uno.Extensions.Reactive.Core.FeedHelper;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A token indicating 
/// </summary>
/// <param name="Source">The source feed that has been refreshed.</param>
/// <param name="RootContextId">The id of the <see cref="SourceContext"/> chain for which the refresh was requested.</param>
/// <param name="Version">
/// A sequential number indicating the version of the data produced by the source feed.
/// This also correspond to the number of times that the source feed has been refreshed.
/// </param>
internal record RefreshToken(IRefreshableSource Source, uint RootContextId, uint Version)
{
	/// <summary>
	/// Creates the initial token for a refreshable feed.
	/// </summary>
	/// <param name="source">The refreshable source feed.</param>
	/// <param name="context">The context used to <see cref="ISignal{T}.GetSource"/> on the <paramref name="source"/>.</param>
	/// <returns>The initial refresh token where <see cref="Version"/> is set to 0.</returns>
	/// <remarks>This token represents the initial load of the <paramref name="source"/> and should not be propagated.</remarks>
	public static RefreshToken Initial(IRefreshableSource source, SourceContext context) => new(source, context.RootId, 0);

	/// <summary>
	/// Atomatically increments a refresh token and returns it.
	/// </summary>
	/// <param name="version">The backing variable that has to be incremented.</param>
	/// <returns>The updated token.</returns>
	public static RefreshToken InterlockedIncrement(ref RefreshToken version)
	{
		while(true)
		{
			var current = version;
			var updated = current with { Version = current.Version + 1 };

			if (Interlocked.CompareExchange(ref version, updated, current) == current)
			{
				return updated;
			}
		}
	}

	/// <inheritdoc />
	public override string ToString()
		=> $"[ctx{RootContextId}] {GetDebugIdentifier(Source)} v{Version}";
}
