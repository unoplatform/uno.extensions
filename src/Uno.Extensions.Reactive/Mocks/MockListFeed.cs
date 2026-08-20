using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Mocks;

/// <summary>
/// Provides factories of <see cref="IListFeed{T}"/> pinned to a fixed message state, for previews and UI tests of visual states.
/// </summary>
/// <remarks>
/// A mocked list feed emits its configured state once and then completes, which pins the subscriber
/// (e.g. a <c>FeedView</c>) in that state without any pending work left behind.
/// </remarks>
public static class MockListFeed
{
	/// <summary>
	/// Gets a list feed pinned in the undefined state, i.e. as if the source had not produced any data yet.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <returns>A list feed pinned in the undefined state.</returns>
	public static IListFeed<T> Undefined<T>()
		=> Wrap(MockFeed.Undefined<IImmutableList<T>>());

	/// <summary>
	/// Gets a list feed pinned in the loading state, indefinitely.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <returns>A list feed pinned in the loading state.</returns>
	public static IListFeed<T> Loading<T>()
		=> Wrap(MockFeed.Loading<IImmutableList<T>>());

	/// <summary>
	/// Gets a list feed pinned in the empty state (<see cref="Option{T}.None"/>), i.e. the "no result" state.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <returns>A list feed pinned in the empty state.</returns>
	public static IListFeed<T> Empty<T>()
		=> Wrap(MockFeed.Empty<IImmutableList<T>>());

	/// <summary>
	/// Gets a list feed pinned with an empty list as data, i.e. <c>Some([])</c> — distinct from <see cref="Empty{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <returns>A list feed pinned with an empty list as data.</returns>
	/// <remarks>
	/// Real list feeds normalize an empty list to <see cref="Option{T}.None"/>; this state is only
	/// producible by a mock and is intended to validate how a view renders a present-but-empty list.
	/// </remarks>
	public static IListFeed<T> EmptyList<T>()
		=> Wrap(MockFeed.Value<IImmutableList<T>>(ImmutableList<T>.Empty));

	/// <summary>
	/// Gets a list feed pinned with the given items.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="items">The items of the list.</param>
	/// <returns>A list feed pinned with the given items.</returns>
	/// <remarks>Like real list feeds, no items produces the empty state; use <see cref="EmptyList{T}"/> to pin <c>Some([])</c>.</remarks>
	public static IListFeed<T> Value<T>(params T[] items)
	{
		items = items ?? throw new ArgumentNullException(nameof(items));

		return Value(items.ToImmutableList() as IImmutableList<T>);
	}

	/// <summary>
	/// Gets a list feed pinned with the given items.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="items">The items of the list.</param>
	/// <returns>A list feed pinned with the given items.</returns>
	/// <remarks>Like real list feeds, an empty list produces the empty state; use <see cref="EmptyList{T}"/> to pin <c>Some([])</c>.</remarks>
	public static IListFeed<T> Value<T>(IImmutableList<T> items)
	{
		items = items ?? throw new ArgumentNullException(nameof(items));

		return items.Count == 0
			? Empty<T>()
			: Wrap(MockFeed.Value(items));
	}

	/// <summary>
	/// Gets a list feed pinned with the given items and a pinned selection.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="items">The items of the list.</param>
	/// <param name="selection">The selection to pin, e.g. <c>SelectionInfo.Single(1)</c>.</param>
	/// <returns>A list feed pinned with the given items and selection.</returns>
	/// <remarks>Like real list feeds, an empty list produces the empty state and the selection is ignored.</remarks>
	public static IListFeed<T> Value<T>(IImmutableList<T> items, SelectionInfo selection)
	{
		items = items ?? throw new ArgumentNullException(nameof(items));
		selection = selection ?? throw new ArgumentNullException(nameof(selection));

		return items.Count == 0
			? Empty<T>()
			: Wrap(MockFeed.Message<IImmutableList<T>>(m => m.Data(items).Selected(selection)));
	}

	/// <summary>
	/// Gets a list feed pinned in the error state.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="error">The error of the feed.</param>
	/// <returns>A list feed pinned in the error state.</returns>
	public static IListFeed<T> Error<T>(Exception error)
		=> Wrap(MockFeed.Error<IImmutableList<T>>(error)); // Null-guarded by MockFeed.Error.

	/// <summary>
	/// Gets a list feed pinned with stale items while refreshing, indefinitely.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="staleItems">The stale items exposed while refreshing.</param>
	/// <returns>A list feed pinned in the refreshing state.</returns>
	/// <remarks>
	/// This is visually faithful (data present with an indeterminate progress) but does not set
	/// the internal refresh axis, which cannot be produced outside of a refreshable source feed.
	/// </remarks>
	public static IListFeed<T> Refreshing<T>(params T[] staleItems)
	{
		staleItems = staleItems ?? throw new ArgumentNullException(nameof(staleItems));

		return staleItems.Length == 0
			? Wrap(MockFeed.Message<IImmutableList<T>>(m => m.Data(Option<IImmutableList<T>>.None()).IsTransient(true)))
			: Wrap(MockFeed.Refreshing<IImmutableList<T>>(staleItems.ToImmutableList()));
	}

	private static IListFeed<T> Wrap<T>(IFeed<IImmutableList<T>> source)
		=> new Adapter<T>(source);

	// Not AsListFeed(): FeedToListFeedAdapter coerces Some(empty list) to None, which would make
	// EmptyList indistinguishable from Empty. Mocked messages are forwarded as-is; the binding layer
	// computes its own collection change-set when a message does not carry one (cf. BindableEnumerable).
	private sealed class Adapter<T> : IListFeed<T>
	{
		private readonly IFeed<IImmutableList<T>> _source;

		public Adapter(IFeed<IImmutableList<T>> source)
			=> _source = source;

		/// <inheritdoc />
		public IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
			=> _source.GetSource(context, ct);
	}
}
