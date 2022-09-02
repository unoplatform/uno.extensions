using System;
using System.Linq;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// Defines the visual state that should be used for refresh.
/// </summary>
public enum FeedViewRefreshState
{
	/// <summary>
	/// Do not use any visual state, this is usually use-full when you have an external refreshing visual state,
	/// like a RefreshContainer (cf. Remarks about limitations).
	/// </summary>
	/// <remarks>
	///	This will disable the refreshing state for refresh sources, no matter who as triggered the refresh.
	/// This means that if you use this flag because you are using a RefreshContainer,
	/// but your source is being refreshed due to another on canvas refresh button or just because a dependency is being refreshed,
	/// you won't have any refresh template neither.
	/// </remarks>
	None,

	/// <summary>
	/// Use the loading visual state while refreshing.
	/// </summary>
	Loading,

	/// <summary>
	/// The default is to use the <see cref="Loading"/> for now.
	/// </summary>
	Default = Loading,
}
