using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Defines how an existing should behave when it's being removed from the owner class during hot reload operations.
/// </summary>
public enum HotReloadRemovalBehavior
{
	/// <summary>
	/// Do nothing.
	/// </summary>
	KeepPrevious,

	/// <summary>
	/// Put the feed in error state.
	/// </summary>
	Error,

	/// <summary>
	/// Clear the feed like if it has not been initialized yet.
	/// </summary>
	Clear
}
