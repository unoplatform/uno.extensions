using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Reactive;

internal interface IListFeedWrapper<T>
{
	IFeed<IImmutableList<T>> Source { get; }
}
