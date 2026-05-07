using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Defines the hot-reload support for the MVUX framework.
/// </summary>
[Flags]
[EditorBrowsable(EditorBrowsableState.Advanced)]
public enum HotReloadSupport
{
	/// <summary>
	/// Globally enables the hot-reload support for all dynamic feed.
	/// </summary>
	DynamicFeed = 1 << 1,

	/// <summary>
	/// Enables the hot-reload support for the states.
	/// This is required for hot-reload support in bindables.
	/// </summary>
	State = 1 << 2,

	/// <summary>
	/// Enables the hot-reload support for async feeds (refresh when the type that declares the data provider is updated).
	/// </summary>
	AsyncFeed = 1 << 3,

	/// <summary>
	/// Globally enables the hot-reload support for the MVUX framework.
	/// </summary>
	Enabled = 255,

	/// <summary>
	/// Disables the hot-reload support for the MVUX framework.
	/// </summary>
	Disabled = 256
}
