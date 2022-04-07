using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

internal record ListState<T>(IState<IImmutableList<T>> implementation) : ListFeed<T>(implementation), IListState<T>
{
	private readonly IState<IImmutableList<T>> implementation = implementation;

	/// <inheritdoc />
	public ValueTask Update(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> implementation.Update(updater, ct);
}
