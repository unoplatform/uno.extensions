using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// Implemented by the mocking layer (<c>Uno.HotTesting.Reactive</c>, spec 013): resolves the source a mocking
/// context subscribes to in order to observe a feed, so feeds derived from a mocked input observe the mock.
/// </summary>
internal interface IMockingSourceResolver
{
	/// <summary>
	/// Gets the source to subscribe to in order to observe <paramref name="feed"/> in <paramref name="context"/>.
	/// </summary>
	/// <typeparam name="T">Type of the value of the feed.</typeparam>
	/// <param name="context">The mocking context that observes the feed.</param>
	/// <param name="feed">The observed feed.</param>
	/// <returns>The source to subscribe to, which may be <paramref name="feed"/> itself.</returns>
	/// <remarks>
	/// The result is used as a subscription key, so an implementation must:
	/// return the same instance for the same store and feed; return an already resolved source unchanged;
	/// be thread-safe and never return null; and never create a state or a subscription, since the store's
	/// subscription lock may be held. It ships with <c>Uno.HotTesting.Reactive</c>: the two change together.
	/// </remarks>
	ISignal<Message<T>> Resolve<T>(SourceContext context, ISignal<Message<T>> feed);
}
