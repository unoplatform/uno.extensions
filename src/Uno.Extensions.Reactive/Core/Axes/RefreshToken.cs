using System;
using System.Diagnostics.Contracts;
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
/// <param name="SequenceId">
/// A sequential number indicating the version of the data produced by the source feed.
/// This also correspond to the number of times that the source feed has been refreshed.
/// </param>
internal record RefreshToken(ISignal<IMessage> Source, uint RootContextId, uint SequenceId) : IToken, IToken<RefreshToken>
{
	/// <inheritdoc />
	object IToken.Source => Source;

	/// <summary>
	/// Creates the initial token for a refreshable feed.
	/// </summary>
	/// <param name="source">The refreshable source feed.</param>
	/// <param name="context">The context used to <see cref="ISignal{T}.GetSource"/> on the <paramref name="source"/>.</param>
	/// <returns>The initial refresh token where <see cref="SequenceId"/> is set to 0.</returns>
	/// <remarks>This token represents the initial load of the <paramref name="source"/> and should not be propagated.</remarks>
	public static RefreshToken Initial(ISignal<IMessage> source, SourceContext context) => new(source, context.RootId, 0);

	/// <summary>
	/// Atomatically increments a refresh token and returns it.
	/// </summary>
	/// <param name="token">The backing variable that has to be incremented.</param>
	/// <returns>The updated token.</returns>
	public static RefreshToken InterlockedIncrement(ref RefreshToken token)
	{
		while(true)
		{
			var current = token;
			var updated = current.Next();

			if (Interlocked.CompareExchange(ref token, updated, current) == current)
			{
				return updated;
			}
		}
	}

	/// <inheritdoc />
	[Pure]
	public RefreshToken Next()
		=> this with { SequenceId = SequenceId + 1 };

	/// <inheritdoc />
	public override string ToString()
		=> $"[ctx{RootContextId}] {GetDebugIdentifier(Source)} v{SequenceId}";
}
