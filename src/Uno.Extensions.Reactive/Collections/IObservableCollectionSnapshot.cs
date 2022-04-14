using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Collections;

/// <summary>
/// A snapshot of an <see cref="IObservableCollection"/>.
/// </summary>
internal interface IObservableCollectionSnapshot : IList
{
	// Note: We force the implementation of the IList interface in order to be able to use the snapshot as a parameter of the collection changed event args

	/// <summary>
	/// Searches for the specified object and returns the zero-based index of the first occurrence within the snapshot.
	/// </summary>
	/// <param name="item">The object to locate in the snapshot.</param>
	/// <param name="startIndex">The zero-based starting index of the search. 0 (zero) is valid in an empty list.</param>
	/// <param name="comparer">The equality comparer to use in the search, or null to use the default comparer.</param>
	/// <returns>The zero-based index of the first occurrence of item within the range of elements in the snapshot that extends from index to the last element, if found; otherwise, -1.</returns>
	int IndexOf(object item, int startIndex, IEqualityComparer? comparer = null);
}
