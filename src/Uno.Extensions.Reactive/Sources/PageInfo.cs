using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Information about a page
/// </summary>
public struct PageInfo
{
	/// <summary>
	/// The index of the page.
	/// </summary>
	public uint PageIndex { get; init; }

	/// <summary>
	/// The desired number of items, if any.
	/// </summary>
	/// <remarks>
	/// This is the desired number of items that the view requested to load.
	/// It's expected to be null only for the first page.
	/// </remarks>
	public uint? DesiredPageSize { get; init; }
}
