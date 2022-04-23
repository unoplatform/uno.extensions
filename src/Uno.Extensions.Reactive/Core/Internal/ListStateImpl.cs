using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Operators;

namespace Uno.Extensions.Reactive;

internal record ListStateImpl<T>(IState<IImmutableList<T>> implementation) : FeedToListFeedAdapter<T>(implementation), IListState<T>
{
	private readonly IState<IImmutableList<T>> implementation = implementation;

	/// <inheritdoc />
	public ValueTask Update(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> implementation.Update(updater, ct);

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> implementation.DisposeAsync();
}
