using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Operators;

internal sealed record ListFeedToFeedAdapter<T>(IListFeed<T> Source) : IFeed<IImmutableList<T>>
{
	/// <inheritdoc />
	public IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
		=> context.GetOrCreateSource(Source);
}
