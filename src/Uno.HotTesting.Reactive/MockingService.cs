using System;
using System.Collections.Immutable;
using System.ComponentModel;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;

namespace Uno.HotTesting.Reactive;

/// <summary>
/// Activates MVUX mocking and applies typed feed swaps (spec 013). Inside an <see cref="Enable"/> scope,
/// every <see cref="SourceContext"/> created (and its descendants) is mockable: its feeds are wrapped so
/// their source can be swapped at runtime. Outside any scope nothing is wrapped, so a live application
/// pays nothing (G9/R7).
/// </summary>
/// <remarks>
/// Granularity is the caller's: open it once at assembly-init to cover a whole test run, or around a
/// single <c>Create(...)</c>. The bit is captured on the context instance at construction, so a lazy first
/// subscription after the scope is disposed still wraps (D12). The swap helpers are strongly typed
/// (the generated <c>SetModel</c> emits concrete calls — no reflection, AOT-friendly) and <b>fail-hard</b>:
/// a feed that is not wrapped (model built outside a scope) throws instead of silently doing nothing.
/// </remarks>
public static class MockingService
{
	/// <summary>
	/// Opens a mocking-activation scope. Dispose it to stop marking future contexts as mockable;
	/// contexts already created inside the scope stay mockable for their own lifetime.
	/// </summary>
	public static IDisposable Enable()
		=> SourceContext.EnableMocking();

	/// <summary>
	/// Swaps the source of a scalar feed member (called by generated <c>SetModel</c>).
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void SwapFeed<T>(object owner, IFeed<T> current, IFeed<T> replacement)
		where T : notnull
	{
		var ctx = SourceContext.GetOrCreate(owner);
		var state = ctx.GetOrCreateState(current);
		if (state is not IHotSwapState<T> hotSwap || !hotSwap.CanHotSwap)
		{
			throw new InvalidOperationException(
				$"The feed for the mocked member is not swappable (no HotSwapFeed wrapper). "
				+ $"Ensure the model was constructed inside a MockingService.Enable() scope. Value type: {typeof(T)}.");
		}

		hotSwap.HotSwap(replacement);
	}

	/// <summary>
	/// Swaps the source of a list-feed member (called by generated <c>SetModel</c>).
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void SwapListFeed<T>(object owner, IListFeed<T> current, IListFeed<T> replacement)
		where T : notnull
	{
		var ctx = SourceContext.GetOrCreate(owner);
		var currentFeed = ListFeed.AsFeed(current);
		var state = ctx.GetOrCreateState(currentFeed);
		if (state is not IHotSwapState<IImmutableList<T>> hotSwap || !hotSwap.CanHotSwap)
		{
			throw new InvalidOperationException(
				$"The list-feed for the mocked member is not swappable (no HotSwapFeed wrapper). "
				+ $"Ensure the model was constructed inside a MockingService.Enable() scope. Item type: {typeof(T)}.");
		}

		hotSwap.HotSwap(ListFeed.AsFeed(replacement));
	}
}
