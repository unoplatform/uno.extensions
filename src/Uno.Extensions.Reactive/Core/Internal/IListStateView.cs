using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A list state that is a view of a state holding the whole list.
/// </summary>
internal interface IListStateView<T>
{
	/// <summary>
	/// Gets the state this list state is a view of.
	/// </summary>
	StateImpl<IImmutableList<T>> Implementation { get; }
}
