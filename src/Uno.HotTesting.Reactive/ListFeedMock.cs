using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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

	/// <summary>Creates a list feed with a present, empty list.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <returns>A list feed whose data axis is <c>Some(empty)</c>.</returns>
	/// <remarks>
	/// This intentionally differs from a scalar feed's <see cref="FeedMock.Empty{T}"/>.
	/// List views need an empty collection value to render their empty state. The mock adapter
	/// preserves that value instead of applying the normal Some(empty)-to-None coercion.
	/// </remarks>
	public static IListFeed<T> Empty<T>()
		=> Wrap(FeedMock.Value<IImmutableList<T>>(ImmutableList<T>.Empty));

	/// <summary>Creates a list feed pinned to the supplied items.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <param name="items">The items to expose.</param>
	/// <returns>A list feed whose data axis contains the supplied items.</returns>
	/// <remarks>An empty array has the same <c>Some(empty)</c> semantics as <see cref="Empty{T}"/>.</remarks>
	public static IListFeed<T> Value<T>(params T[] items)
	{
		if (items is null)
		{
			throw new ArgumentNullException(nameof(items));
		}

		return Wrap(FeedMock.Value<IImmutableList<T>>(items.ToImmutableList()));
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
	/// An empty array remains <c>Some(empty)</c>. The internal refresh axis can only be
	/// raised by a refreshable source feed.
	/// </remarks>
	public static IListFeed<T> Refreshing<T>(params T[] staleItems)
	{
		if (staleItems is null)
		{
			throw new ArgumentNullException(nameof(staleItems));
		}

		return Wrap(FeedMock.Refreshing<IImmutableList<T>>(staleItems.ToImmutableList()));
	}

	/// <summary>Creates a list feed pinned to an arbitrary message.</summary>
	/// <typeparam name="T">The list item type.</typeparam>
	/// <param name="configure">Configures the message axes.</param>
	/// <returns>A list feed which emits the configured message and completes.</returns>
	public static IListFeed<T> Message<T>(Action<MessageBuilder<IImmutableList<T>>> configure)
		=> Wrap(FeedMock.Message(configure));

	private static IListFeed<T> Wrap<T>(IFeed<IImmutableList<T>> source)
		=> new Adapter<T>(source);

	// AsListFeed normalizes Some(empty) to None. A mock must preserve the caller's
	// explicit message so a list-empty visual state remains expressible.
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
