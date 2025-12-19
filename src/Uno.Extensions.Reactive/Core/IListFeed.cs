using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A **stateless** stream of list of items.
/// </summary>
/// <typeparam name="T">The type of the items in the list.</typeparam>
public interface IListFeed<
	[DynamicallyAccessedMembers(ListFeed.TRequirements)]
	T
> : ISignal<Message<IImmutableList<T>>>
{
}
