using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Collections;

/// <summary>
/// Information of a page of items.
/// </summary>
internal interface IPageContent<T> : IPageContent
{
	/// <summary>
	/// The items of the page
	/// </summary>
	IImmutableList<T> Items { get; }
}
