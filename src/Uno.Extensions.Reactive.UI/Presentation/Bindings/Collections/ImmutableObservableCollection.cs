using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.Extensions.Collections;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Bindings.Collections;

internal class ImmutableObservableCollection<T> : IReadOnlyObservableCollection<T>, IObservableCollectionSnapshot<T>
{
	private readonly IImmutableList<T> _inner;

	public ImmutableObservableCollection(IImmutableList<T> inner)
	{
		_inner = inner;
	}

	/// <inheritdoc />
	public int Count => _inner.Count;

	/// <inheritdoc />
	public bool IsFixedSize => true;

	/// <inheritdoc />
	public bool IsReadOnly => true;

	/// <inheritdoc />
	public bool IsSynchronized => true;

	/// <inheritdoc />
	public object SyncRoot { get; } = new();

	/// <inheritdoc />
	public T this[int index]
	{
		get => _inner[index];
		set => throw NotSupported();
	}

	/// <inheritdoc />
	object? IList.this[int index]
	{
		get => ((IList)_inner)[index];
		set => throw NotSupported();
	}

	#region Write (Not Supported / explicit implementations)
	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	int IList.Add(object? value) => throw NotSupported();
	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	void ICollection<T>.Add(T item) => throw NotSupported();

	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	void IObservableCollection<T>.AddRange(IReadOnlyList<T> items) => throw NotSupported();

	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	void IObservableCollection<T>.ReplaceRange(int index, int count, IReadOnlyList<T> newItems) => throw NotSupported();

	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	void IList.Insert(int index, object? value) => throw NotSupported();

	/// <inheritdoc />
	public int IndexOf(T item)
		=> _inner.IndexOf(item);

	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	void IList<T>.Insert(int index, T item) => throw NotSupported();

	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	void IList.Remove(object? value) => throw NotSupported();

	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	bool IObservableCollection.Remove(object? value) => throw NotSupported();

	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	bool ICollection<T>.Remove(T item) => throw NotSupported();

	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	public void RemoveAt(int index) => throw NotSupported();

	/// <summary>Not supported on read only collection</summary>
	/// <exception cref="NotSupportedException">This method is not supported on this collection, a <see cref="NotSupportedException"/> will be thrown.</exception>
	public void Clear() => throw NotSupported();
	#endregion

	private InvalidOperationException NotSupported([CallerMemberName] string? method = null)
		=> new($"Cannot '{method}' on a ReadOnlyCollection.");

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
		=> _inner.GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> ((IEnumerable)_inner).GetEnumerator();

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
		=> ((ICollection<T>)_inner).CopyTo((T[])array, index);

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex)
		=> ((ICollection<T>)_inner).CopyTo(array, arrayIndex);

	/// <inheritdoc />
	public bool Contains(T item)
		=> _inner.Contains(item);

	/// <inheritdoc />
	public bool Contains(object? value)
		=> _inner.Contains((T)value!);

	/// <inheritdoc />
	public int IndexOf(object? value)
		=> _inner.IndexOf((T)value!);

	/// <inheritdoc />
	public int IndexOf(object? item, int startIndex, IEqualityComparer? comparer = null)
		=> _inner.IndexOf((T)item!, startIndex, Count, comparer?.ToEqualityComparer<T>());

	/// <inheritdoc />
	public int IndexOf(T item, int startIndex, IEqualityComparer<T>? comparer = null)
		=> _inner.IndexOf(item, startIndex, Count, comparer);

#pragma warning disable CS0067
	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler? CollectionChanged;
#pragma warning restore CS0067

	/// <inheritdoc />
	public IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
	{
		current = this;
		return Disposable.Empty;
	}

	/// <inheritdoc />
	public IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current)
	{
		current = this;
		return Disposable.Empty;
	}

	/// <inheritdoc />
	public void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
		=> current = this;

	/// <inheritdoc />
	public void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current)
		=> current = this;
}

///// <summary>
///// A read-write list input view model
///// </summary>
///// <typeparam name="T">Type of items of the list</typeparam>
//internal class ListInputViewModel<T> : ListFeedViewModelBase<T>, IListInput<T>, IInput<IImmutableList<T>>
//{

//}
