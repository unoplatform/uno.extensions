using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Collections;

/// <summary>
/// A collection of <typeparamref name="T"/> which notifies read and write oprations.
/// </summary>
/// <typeparam name="T">Type of items</typeparam>
internal interface IObservableCollection<T> : IObservableCollection, IList<T>
{
	/// <summary>
	/// Add a range of items into the collection
	/// </summary>
	/// <param name="items">Items to add</param>
	void AddRange(IReadOnlyList<T> items);

	/// <summary>
	/// Replace a range of <paramref name="count"/> items starting at <paramref name="index"/> by another set of items.
	/// <remarks>This is equivalent to RemoveRange() and InsertRange()</remarks>
	/// </summary>
	/// <param name="index">The starting index to begin replacement.</param>
	/// <param name="count">The number of elements to replace.</param>
	/// <param name="newItems">The elements to insert to replace removed items.</param>
	void ReplaceRange(int index, int count, IReadOnlyList<T> newItems);

	/// <summary>
	/// Atomatically adds a <see cref="INotifyCollectionChanged.CollectionChanged"/> event handler from any thread for a given scheduling context.
	/// <remarks>The handler is attended to be invoked on the provided scheduling context.</remarks>
	/// </summary>
	/// <param name="callback">The event handler.</param>
	/// <param name="current">The items when the handler was added.</param>
	/// <returns>
	/// An <see cref="IDisposable"/> which removes the handler when disposed.
	/// <remarks>Alternatively, you can use the <see cref="RemoveCollectionChangedHandler"/> method.</remarks>
	/// </returns>
	IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current);


	/// <summary>
	/// Atomatically removes a <see cref="INotifyCollectionChanged.CollectionChanged"/> event handler from any thread for a given scheduling context.
	/// <remarks>
	/// If you don't need to retreive the <paramref name="current"/> version of the collection, you should use the <see cref="IDisposable"/> returned by the <see cref="AddCollectionChangedHandler"/>.
	/// </remarks>
	/// </summary>
	/// <param name="callback">The event handler.</param>
	/// <param name="current">The items when the handler was added.</param>
	void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current);

	#region Disambiguation
	/// <summary>
	/// Gets the number of elements in the collection.
	/// </summary>
	new int Count { get; }

	/// <summary>
	/// Gets a value indicating whether the <see cref="IList"/> is read-only.
	/// </summary>
	new bool IsReadOnly { get; }

	/// <summary>
	/// Gets the element of the set at the given index.
	/// </summary>
	/// <param name="index">The 0-based index of the element in the set to return.</param>
	/// <returns>The element at the given position.</returns>
	new T this[int index] { get; set; }
		
	/// <summary>
	/// Removes all items from the <see cref="IList"/>.
	/// </summary>
	new void Clear();

	/// <summary>
	/// Removes the <see cref="IList"/> item at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the item to remove.</param>
	new void RemoveAt(int index);
	#endregion
}
