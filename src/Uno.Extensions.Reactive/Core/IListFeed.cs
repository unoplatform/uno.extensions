using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A **stateless** stream of list of items.
/// </summary>
/// <typeparam name="T">The type of the items in the list.</typeparam>
public interface IListFeed<T> : ISignal<Message<IImmutableList<T>>>
{
}

internal interface IListFeedWrapper<T>
{
	IFeed<IImmutableList<T>> Source { get; }
}
