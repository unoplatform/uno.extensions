using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

internal record ListFeed<T>(IFeed<IImmutableList<T>> _implementation) : IListFeed<T>, IListFeedWrapper<T>
{
	private readonly IFeed<IImmutableList<T>> _implementation = _implementation;

	IFeed<IImmutableList<T>> IListFeedWrapper<T>.Source => _implementation;

	/// <inheritdoc />
	public IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
		=> _implementation.GetSource(context, ct);
}
