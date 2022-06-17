using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Information about a page
/// </summary>
public struct PageRequest
{
	/// <summary>
	/// The index of the page.
	/// </summary>
	public uint Index { get; init; }

	/// <summary>
	/// This is the total number of items currently in the list.
	/// </summary>
	public uint CurrentCount { get; init; }

	/// <summary>
	/// The desired number of items for the current page, if any.
	/// </summary>
	/// <remarks>
	/// This is the desired number of items that the view requested to load.
	/// It's expected to be null only for the first page.
	/// Be aware that this might change between pages (especially is user resize the window),
	/// DO NOT use like `source.Skip(page.Index * page.DesiredSize).Take(page.DesiredSize)`.
	/// Prefer to use the <see cref="CurrentCount"/> like `source.Skip(page.CurrentCount).Take(page.DesiredSize)`.
	/// </remarks>
	public uint? DesiredSize { get; init; }
}
