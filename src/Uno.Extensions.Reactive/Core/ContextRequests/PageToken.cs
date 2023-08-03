using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A token issue by a feed in response to a <see cref="PageRequest"/>.
/// </summary>
/// <param name="Source">The source that is able to react to the page request.</param>
/// <param name="RootContextId"><inheritdoc cref="IToken.RootContextId"/></param>
/// <param name="SequenceId"><inheritdoc cref="IToken.SequenceId"/></param>
internal record PageToken(ISignal<IMessage> Source, uint RootContextId, uint SequenceId) : IToken, IToken<PageToken>
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
	public static PageToken Initial(ISignal<IMessage> source, SourceContext context) => new(source, context.RootId, 0);

	/// <inheritdoc />
	[Pure]
	public PageToken Next()
		=> this with { SequenceId = SequenceId + 1 };
}
