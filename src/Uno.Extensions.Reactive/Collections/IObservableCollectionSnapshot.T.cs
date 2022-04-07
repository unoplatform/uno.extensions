using System;
using System.Collections.Generic;
using System.Linq;

namespace nVentive.Umbrella.Collections
{
	/// <summary>
	/// A snapshot of an <see cref="IObservableCollection{T}"/>.
	/// </summary>
	/// <typeparam name="T">Type of the items in the collection</typeparam>
	public interface IObservableCollectionSnapshot<T> : IObservableCollectionSnapshot, IReadOnlyList<T>
	{
		/// <summary>
		/// Searches for the specified object and returns the zero-based index of the first occurrence within the snapshot.
		/// </summary>
		/// <param name="item">The object to locate in the snapshot.</param>
		/// <param name="startIndex">The zero-based starting index of the search. 0 (zero) is valid in an empty list.</param>
		/// <param name="comparer">The equality comparer to use in the search, or null to use the default comparer.</param>
		/// <returns>The zero-based index of the first occurrence of item within the range of elements in the snapshot that extends from index to the last element, if found; otherwise, -1.</returns>
		int IndexOf(T item, int startIndex, IEqualityComparer<T>? comparer = null);

		#region Disambiguation
		/// <summary>
		/// Gets the number of elements in the collection.
		/// </summary>
		new int Count { get; }

		/// <summary>
		/// Gets the element of the set at the given index.
		/// </summary>
		/// <param name="index">The 0-based index of the element in the set to return.</param>
		/// <returns>The element at the given position.</returns>
		new T this[int index] { get; }
		#endregion
	}
}
