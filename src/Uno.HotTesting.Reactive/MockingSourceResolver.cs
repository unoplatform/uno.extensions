using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;

namespace Uno.HotTesting.Reactive;

/// <summary>
/// Gives every feed observed in a mocking context one swap layer per state store (spec 013, D6). The feed's state
/// and every feed derived from it subscribe to the layer instead of the feed, so swapping the layer is how
/// <see cref="MockingService"/> puts a mock in place.
/// </summary>
/// <remarks>
/// Lock-free with respect to the state store: <see cref="Resolve{T}"/> runs while a subscription is being created.
/// </remarks>
internal sealed class MockingSourceResolver : IMockingSourceResolver
{
	public static MockingSourceResolver Instance { get; } = new();

	private readonly ConditionalWeakTable<object, ConcurrentDictionary<object, object>> _layersPerStore = new();

	/// <inheritdoc />
	public ISignal<Message<T>> Resolve<T>(SourceContext context, ISignal<Message<T>> feed)
		=> feed is IState or HotSwapFeed<T> or UnroutedFeed<T>
			? feed // a state (list states included) is observed as-is, and a layer or its input must never be routed to itself
			: GetOrCreateLayer(context, feed);

	/// <summary>
	/// Gets the swap layer through which <paramref name="context"/> observes <paramref name="feed"/>.
	/// </summary>
	public HotSwapFeed<T> GetOrCreateLayer<T>(SourceContext context, ISignal<Message<T>> feed)
		=> (HotSwapFeed<T>)_layersPerStore
			.GetValue(context.States, static _ => new())
			.GetOrAdd(feed, static f => new HotSwapFeed<T>(new UnroutedFeed<T>((ISignal<Message<T>>)f)));
}
