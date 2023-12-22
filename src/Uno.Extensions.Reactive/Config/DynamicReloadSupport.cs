using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Defines the dynamic reload support for the MVUX framework.
/// </summary>
[Flags]
internal enum DynamicReloadSupport
{
	/// <summary>
	/// Enables dynamic reload for the Feed.Async source feed.
	/// </summary>
	Async = 1 << 1,

	/// <summary>
	/// Enables dynamic reload for feeds created using the Feed.SelectAsync operator.
	/// </summary>
	SelectAsync = 1 << 2,

	/// <summary>
	/// Enables the dynamic reload feature for all supported feeds.
	/// </summary>
	All = 255
}
