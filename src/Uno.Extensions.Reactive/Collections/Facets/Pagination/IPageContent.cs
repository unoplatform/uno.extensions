using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Collections;

/// <summary>
/// Information of a page of items.
/// </summary>
internal interface IPageContent
{
	/// <summary>
	/// The number of items that was actually loaded.
	/// </summary>
	uint Count { get; }

	/// <summary>
	/// Gets a boolean which indicates if more pages are available.
	/// </summary>
	bool HasMoreItems { get; }
}
