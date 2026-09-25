using System;
using System.Collections.Generic;
using System.Threading;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;

namespace Uno.HotTesting.Reactive;

/// <summary>
/// The original input of a swap layer: subscribes to <paramref name="Inner"/> without going through
/// <see cref="SourceContext.GetOrCreateSource{T}"/>, which would route it back to the layer itself.
/// </summary>
/// <param name="Inner">The feed the layer wraps.</param>
internal sealed record UnroutedFeed<T>(ISignal<Message<T>> Inner) : IFeed<T>
{
	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
		=> context.States.GetOrCreateSubscription(Inner).GetMessages(context, ct);
}
