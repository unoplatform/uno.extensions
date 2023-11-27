using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;

namespace Uno.Extensions.Reactive;

// Note: This should **not** be a record as it causes some issues with mono runtime for Android and iOS
// which crashes when using instances of this record in dictionaries (caching) and break the AOT build.
// cf. https://github.com/unoplatform/Uno.Samples/issues/139

// Note: This is a "quick implementation". Inheriting from FeedToListFeedAdapter is most probably not a good idea.

internal class ListStateImpl<T> : FeedToListFeedAdapter<T>, IListState<T>, IStateImpl
{
	private readonly StateImpl<IImmutableList<T>> _implementation;

	public ListStateImpl(StateImpl<IImmutableList<T>> implementation, ItemComparer<T> itemComparer = default)
		: base(implementation, itemComparer)
	{
		_implementation = implementation;
	}

	internal Message<IImmutableList<T>> Current => _implementation.Current;

	/// <inheritdoc />
	public SourceContext Context => _implementation.Context;

	/// <inheritdoc />
	public ValueTask UpdateMessageAsync(Action<MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _implementation.UpdateMessageAsync(updater, ct);

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> _implementation.DisposeAsync();

	/// <inheritdoc />
	public override int GetHashCode()
		=> base.GetHashCode() ^ _implementation.GetHashCode();

	/// <inheritdoc />
	public override bool Equals(object? obj)
		=> obj is ListStateImpl<T> other && Equals(this, other);

	public static bool operator ==(ListStateImpl<T> left, ListStateImpl<T> right)
		=> Equals(left, right);

	public static bool operator !=(ListStateImpl<T> left, ListStateImpl<T> right)
		=> Equals(left, right);

	private static bool Equals(ListStateImpl<T> left, ListStateImpl<T> right)
		=> FeedToListFeedAdapter<IImmutableList<T>, T>.Equals(left, right)
			&& left.Context.Equals(right.Context);
}
