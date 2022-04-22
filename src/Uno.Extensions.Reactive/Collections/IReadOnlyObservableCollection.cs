using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Collections;

/// <summary>
/// Marker interface to identify an <see cref="IObservableCollection{T}"/> which does not allow write operations.
/// </summary>
/// <typeparam name="T">Tuype of items of the collection</typeparam>
internal interface IReadOnlyObservableCollection<T> : IObservableCollection<T>, IReadOnlyList<T>
{
	// Note: We implements IObservableCollection<T> because we must implement IList and IList<T> for the ListView
	//		 so we cannot strip off any interfaces of the IObservableCollection<T> contract.
	//
	//		 Also, we don't want that collection which implements IObservableCollection<T> implement the IReadOnlyList<T>, so we cannot invert inheritance.
	//		 And we don't want to break inheritance in order to ease wrapping of IReadOnlyCollection<T> whithout having to wrap it first (cf. MapObservableCollection).

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
	new T this[int index] { get; set; }
	#endregion
}
