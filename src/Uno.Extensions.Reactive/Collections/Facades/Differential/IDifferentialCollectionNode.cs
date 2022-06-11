using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// An immutable node of a linked list of a differential collection
/// </summary>
internal interface IDifferentialCollectionNode
{
	/// <summary>
	/// Gets the previous node onto which this node has been happened, if any.
	/// </summary>
	IDifferentialCollectionNode? Previous { get; }

	/// <summary>
	/// Gets the number of items currently in the collection 
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Retrieve the item currently at the given index
	/// </summary>
	/// <param name="index">Index of the desired item</param>
	/// <returns>The item which is currently at the given index.</returns>
	/// <exception cref="IndexOutOfRangeException">If index is below 0 or greater than Count.</exception>
	object? ElementAt(int index);

	/// <summary>
	/// Gets the current index of an item in the collection
	/// </summary>
	/// <param name="element">The item to search for.</param>
	/// <param name="startingAt">The index at which search should start</param>
	/// <param name="comparer">An equality comparer to determine equality of the item</param>
	/// <returns>The current index of the items if present in collection, otherwise -1.</returns>
	int IndexOf(object? element, int startingAt, IEqualityComparer? comparer = null);
}
