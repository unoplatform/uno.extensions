using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Uno.Extensions;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;

namespace Uno.HotTesting.Reactive;

/// <summary>
/// Creates finite <see cref="IListFeed{T}"/> instances pinned to common MVUX message states.
/// </summary>
public static class ListFeedMock
{
	/// <summary>Creates a list feed pinned before its first value.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <returns>A list feed whose data axis is undefined.</returns>
	public static IListFeed<T> Undefined<T>()
		=> Wrap(FeedMock.Undefined<IImmutableList<T>>());

	/// <summary>Creates a list feed pinned in an indefinite loading state.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <returns>A transient list feed which completes after its loading message.</returns>
	public static IListFeed<T> Loading<T>()
		=> Wrap(FeedMock.Loading<IImmutableList<T>>());

	/// <summary>Creates a list feed with no items, as when its service returns an empty list.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <returns>A list feed whose data axis is <see cref="Option{T}.None"/>.</returns>
	/// <remarks>
	/// A list feed reports an empty list as no data, so a <c>FeedView</c> shows its
	/// <c>NoneTemplate</c> and feeds derived from this one get no value.
	/// </remarks>
	public static IListFeed<T> Empty<T>()
		=> Wrap(FeedMock.Empty<IImmutableList<T>>());

	/// <summary>Creates a list feed pinned to the supplied items.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <param name="items">The items to expose.</param>
	/// <returns>A list feed whose data axis contains the supplied items.</returns>
	/// <remarks>With no items, the data axis is <see cref="Option{T}.None"/>, as for <see cref="Empty{T}"/>.</remarks>
	public static IListFeed<T> Value<T>(params T[] items)
	{
		if (items is null)
		{
			throw new ArgumentNullException(nameof(items));
		}

		return Wrap(FeedMock.Message<IImmutableList<T>>(message => message.Data(ToData(items))));
	}

	/// <summary>Creates a list feed pinned to an error.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <param name="error">The error to expose.</param>
	/// <returns>A list feed whose error axis contains <paramref name="error"/>.</returns>
	public static IListFeed<T> Error<T>(Exception error)
		=> Wrap(FeedMock.Error<IImmutableList<T>>(error));

	/// <summary>Creates a list feed pinned to stale items while a refresh is in progress.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <param name="staleItems">The stale items to expose.</param>
	/// <returns>A transient list feed with data.</returns>
	/// <remarks>
	/// With no items, the data axis is <see cref="Option{T}.None"/>. The internal refresh axis
	/// can only be raised by a refreshable source feed.
	/// </remarks>
	public static IListFeed<T> Refreshing<T>(params T[] staleItems)
	{
		if (staleItems is null)
		{
			throw new ArgumentNullException(nameof(staleItems));
		}

		return Wrap(FeedMock.Message<IImmutableList<T>>(message => message
			.Data(ToData(staleItems))
			.IsTransient(true)));
	}

	/// <summary>Creates a list feed pinned to an arbitrary message.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <param name="configure">Configures the message axes.</param>
	/// <returns>A list feed which emits the configured message and completes.</returns>
	/// <remarks>
	/// The message is forwarded as configured: unlike the other factories, an explicit empty list
	/// stays <c>Some(empty)</c>, a state no list feed of a running app produces.
	/// </remarks>
	public static IListFeed<T> Message<T>(Action<MessageBuilder<IImmutableList<T>>> configure)
		=> Wrap(FeedMock.Message(configure));

	// Same rule as a real list feed (FeedToListFeedAdapter): an empty list is no data.
	private static Option<IImmutableList<T>> ToData<T>(T[] items)
		=> items.Length == 0
			? Option<IImmutableList<T>>.None()
			: Option<IImmutableList<T>>.Some(items.ToImmutableList());

	private static IListFeed<T> Wrap<T>(IFeed<IImmutableList<T>> source)
		=> new Adapter<T>(source);

	// Forwards messages as built. AsListFeed would subscribe through the context, which
	// replays only the last of Undefined's two messages, and would rewrite Message's data.
	private sealed class Adapter<T> : IListFeed<T>
	{
		private readonly IFeed<IImmutableList<T>> _source;

		public Adapter(IFeed<IImmutableList<T>> source)
		{
			_source = source;
		}

		/// <inheritdoc />
		public IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(
			SourceContext context,
			CancellationToken ct = default)
			=> _source.GetSource(context, ct);
	}
}
