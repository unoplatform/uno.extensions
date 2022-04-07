using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Umbrella.Feeds.Collections.Extensions
{
	/// <summary>
	/// Information of a page of items.
	/// </summary>
	public interface IPageContent
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

	/// <summary>
	/// Information of a page of items.
	/// </summary>
	public interface IPageContent<T> : IPageContent
	{
		/// <summary>
		/// The items of the page
		/// </summary>
		IImmutableList<T> Items { get; }
	}
}
