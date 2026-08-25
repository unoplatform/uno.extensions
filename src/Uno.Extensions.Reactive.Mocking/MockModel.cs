using System;
using System.Collections.Immutable;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Mocking;

/// <summary>
/// Runtime swap engine for MVUX mocking (spec 013, D11). Replaces the source of a model feed member at
/// its cache-level <c>HotSwapFeed</c> wrapper, reusing the hot-reload swap seam (<c>IHotSwapState&lt;T&gt;</c>).
/// The generated <c>SetModel</c> (tier 2/3) emits strongly-typed calls into these helpers.
/// </summary>
/// <remarks>
/// Fail-hard (delta vs hot-reload's best-effort): if the member's feed is not wrapped — i.e. the model
/// was not constructed inside a <see cref="MockingService"/> scope — the swap throws instead of silently
/// doing nothing.
/// </remarks>
public static class MockModel
{
	/// <summary>
	/// Swaps the source of a scalar feed member.
	/// </summary>
	/// <param name="owner">The model (or view-model) instance owning the feed.</param>
	/// <param name="current">The feed currently exposed by the member (the cached wrapper).</param>
	/// <param name="replacement">The mock feed to swap in.</param>
	public static void SwapFeed<T>(object owner, IFeed<T> current, IFeed<T> replacement)
		where T : notnull
	{
		var ctx = SourceContext.GetOrCreate(owner);
		var state = ctx.GetOrCreateState(current);
		if (state is not IHotSwapState<T> hotSwap)
		{
			throw new InvalidOperationException(
				$"The feed for the mocked member is not swappable (no HotSwapFeed wrapper). "
				+ $"Ensure the model was constructed inside a MockingService.Enable() scope. Value type: {typeof(T)}.");
		}

		hotSwap.HotSwap(replacement);
	}

	/// <summary>
	/// Swaps the source of a list-feed member.
	/// </summary>
	public static void SwapListFeed<T>(object owner, IListFeed<T> current, IListFeed<T> replacement)
		where T : notnull
	{
		var ctx = SourceContext.GetOrCreate(owner);
		var currentFeed = ListFeed.AsFeed(current);
		var state = ctx.GetOrCreateState(currentFeed);
		if (state is not IHotSwapState<IImmutableList<T>> hotSwap)
		{
			throw new InvalidOperationException(
				$"The list-feed for the mocked member is not swappable (no HotSwapFeed wrapper). "
				+ $"Ensure the model was constructed inside a MockingService.Enable() scope. Item type: {typeof(T)}.");
		}

		hotSwap.HotSwap(ListFeed.AsFeed(replacement));
	}
}
