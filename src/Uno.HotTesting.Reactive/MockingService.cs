using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Threading;
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
/// This service owns the ambient activation state (an <see cref="AsyncLocal{T}"/>) and registers a probe
/// on <see cref="SourceContext"/> so a newly created context captures its mockability at construction —
/// the bit lives on the context instance, so a lazy first subscription after the scope is disposed still
/// wraps (D12). The swap helpers are strongly typed (the generated <c>SetModel</c> emits concrete calls —
/// no reflection, AOT-friendly) and <b>fail-hard</b>: a feed that is not wrapped (model built outside a
/// scope) throws instead of silently doing nothing.
/// </remarks>
public static class MockingService
{
	private static readonly AsyncLocal<bool> _ambient = new();

	static MockingService()
	{
		// Register the probe Core reads at context creation. Registered only once the mocking layer is
		// touched (i.e. Enable() has been called) — a live app never touches this type, so Core's probe
		// stays null and no context is ever wrapped.
		SourceContext.IsMockingActiveProbe = static () => _ambient.Value;
	}

	/// <summary>
	/// Opens a mocking-activation scope. Dispose it to stop marking future contexts as mockable;
	/// contexts already created inside the scope stay mockable for their own lifetime.
	/// </summary>
	public static IDisposable Enable()
	{
		var previous = _ambient.Value;
		_ambient.Value = true;
		return new Scope(previous);
	}

	private sealed class Scope : IDisposable
	{
		private readonly bool _previous;
		private bool _disposed;

		public Scope(bool previous) => _previous = previous;

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_ambient.Value = _previous;
			}
		}
	}

	/// <summary>
	/// Swaps the source of a scalar feed member (called by generated <c>SetModel</c>).
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void SwapFeed<T>(object owner, IFeed<T> current, IFeed<T> replacement)
		where T : notnull
	{
		var ctx = SourceContext.GetOrCreate(owner);
		if (current is StateImpl<T> currentState && replacement is StateImpl<T> replacementState)
		{
			currentState.HotSwap(replacementState);
		}

		SwapSubscription(ctx, current, replacement, $"Value type: {typeof(T)}.");
	}

	/// <summary>
	/// Swaps the source of a list-feed member (called by generated <c>SetModel</c>).
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void SwapListFeed<T>(object owner, IListFeed<T> current, IListFeed<T> replacement)
		where T : notnull
	{
		var ctx = SourceContext.GetOrCreate(owner);
		if (current is ListStateImpl<T> currentState && replacement is ListStateImpl<T> replacementState)
		{
			currentState.HotSwap(replacementState);
		}

		SwapSubscription<IImmutableList<T>>(ctx, current, replacement, $"Item type: {typeof(T)}.");
	}

	// The member's state and the feeds derived from it all read the feed through this one subscription.
	private static void SwapSubscription<T>(SourceContext ctx, ISignal<Message<T>> current, ISignal<Message<T>> replacement, string typeInfo)
	{
		var subscription = ctx.States.GetOrCreateSubscription(current);
		if (!subscription.CanHotSwap)
		{
			throw new InvalidOperationException(
				$"The feed for the mocked member is not swappable. "
				+ $"Ensure the model was constructed inside a MockingService.Enable() scope. {typeInfo}");
		}

		subscription.HotSwap(replacement);
	}
}
