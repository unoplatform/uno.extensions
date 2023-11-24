using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections;

/// <summary>
/// Default implementation of <see cref="IObservableCollectionSnapshot{T}"/>
/// </summary>
/// <typeparam name="T">Type of the items in the collection.</typeparam>
internal sealed class ObservableCollectionSnapshot<T> : IObservableCollectionSnapshot<T>
{
	private readonly IImmutableList<T> _items;

	public ObservableCollectionSnapshot(IImmutableList<T> items)
	{
		_items = items;
	}

	/// <inheritdoc />
	public int Count => _items.Count;

	/// <inheritdoc />
	public T this[int index] => _items[index];
	object? IList.this[int index]
	{
		get => _items[index]!;
		set => throw NotSupported();
	}

	bool IList.Contains(object? value) => ((IList) _items).Contains(value);

	/// <inheritdoc />
	public int IndexOf(T item, int startIndex, IEqualityComparer<T>? comparer = null) => comparer == null 
		? _items.IndexOf(item, startIndex) 
		: _items.IndexOf(item, startIndex, _items.Count - startIndex, comparer);
	int IList.IndexOf(object? value) => ((IList)_items).IndexOf(value);
	int IObservableCollectionSnapshot.IndexOf(object item, int startIndex, IEqualityComparer? comparer) => comparer is null
		? _items.IndexOf((T)item, startIndex)
		: _items.IndexOf((T)item, startIndex, _items.Count - startIndex, comparer.ToEqualityComparer<T>());

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();

	#region IList/ICollection noise + Write operation (NotSupported)
	bool ICollection.IsSynchronized => ((ICollection)_items).IsSynchronized;
	object ICollection.SyncRoot => ((ICollection)_items).SyncRoot;
	bool IList.IsFixedSize => ((IList)_items).IsFixedSize;
	bool IList.IsReadOnly => ((IList)_items).IsReadOnly;

	void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

	int IList.Add(object? value) => throw NotSupported();
	void IList.Insert(int index, object? value) => throw NotSupported();
	void IList.RemoveAt(int index) => throw NotSupported();
	void IList.Remove(object? value) => throw NotSupported();
	void IList.Clear() => throw NotSupported();

	private NotSupportedException NotSupported([CallerMemberName] string? method = null)
		=> new NotSupportedException($"{method} not supported on a read only list.");
	#endregion
}
