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
	/// <remarks>Called while the store's subscription lock may be held: it must not create a state or a subscription.</remarks>
	ISignal<Message<T>> Resolve<T>(SourceContext context, ISignal<Message<T>> feed);
}
