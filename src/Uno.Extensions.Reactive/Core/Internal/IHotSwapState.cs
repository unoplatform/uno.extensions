using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A state that supports hot swapping of the source.
/// This is intended to be used only for hot-reload purposes.
/// </summary>
/// <typeparam name="T"></typeparam>
internal interface IHotSwapState<T> : IState<T>
{
	/// <summary>
	/// Hot swap the source of this state.
	/// </summary>
	/// <param name="source">The new source.</param>
	void HotSwap(IFeed<T>? source);
}
