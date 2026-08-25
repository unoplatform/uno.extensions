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
	/// Gets a value indicating whether this state is actually backed by a hot-swap wrapper
	/// (i.e. it was created while wrapping was enabled — hot-reload or mocking). When false,
	/// <see cref="HotSwap"/> is a no-op; mocking uses this to fail-hard (spec 013, D11).
	/// </summary>
	bool CanHotSwap { get; }

	/// <summary>
	/// Hot swap the source of this state.
	/// </summary>
	/// <param name="source">The new source.</param>
	void HotSwap(IFeed<T>? source);
}
