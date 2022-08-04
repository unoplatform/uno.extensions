using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;

namespace Uno.Extensions.Reactive;

// Note: This should **not** be a record as it causes some issues with mono runtime for Android and iOS
// which crashes when using instances of this record in dictionaries (caching) and break the AOT build.
// cf. https://github.com/unoplatform/Uno.Samples/issues/139

internal class ListStateImpl<T> : FeedToListFeedAdapter<T>, IListState<T>, IStateImpl
{
	private readonly IState<IImmutableList<T>> _implementation;

	public ListStateImpl(IState<IImmutableList<T>> implementation)
		: base(implementation)
	{
		_implementation = implementation;
	}

	/// <inheritdoc />
	public SourceContext Context => ((IStateImpl)_implementation).Context;

	/// <inheritdoc />
	public ValueTask UpdateMessage(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _implementation.UpdateMessage(updater, ct);

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> _implementation.DisposeAsync();

	/// <inheritdoc />
	public override int GetHashCode()
		=> base.GetHashCode() ^ _implementation.GetHashCode();

	/// <inheritdoc />
	public override bool Equals(object obj)
		=> obj is ListStateImpl<T> other && Equals(this, other);

	public static bool operator ==(ListStateImpl<T> left, ListStateImpl<T> right)
		=> Equals(left, right);

	public static bool operator !=(ListStateImpl<T> left, ListStateImpl<T> right)
		=> Equals(left, right);

	private static bool Equals(ListStateImpl<T> left, ListStateImpl<T> right)
		=> FeedToListFeedAdapter<IImmutableList<T>, T>.Equals(left, right)
			&& left.Context.Equals(right.Context);
}
