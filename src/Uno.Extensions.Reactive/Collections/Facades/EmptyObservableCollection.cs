using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections;

/// <summary>
/// A <see cref="IObservableCollection{T}"/> which contains no items.
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed class EmptyObservableCollection<T> : IReadOnlyObservableCollection<T>
{
	/// <summary>
	/// The singleton instance of the empty collection
	/// </summary>
	public static EmptyObservableCollection<T> Instance { get; } = new();

	private EmptyObservableCollection()
	{
	}

	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler? CollectionChanged
	{
		add { }
		remove { }
	}

	/// <inheritdoc />
	public IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
	{
		current = new ObservableCollectionSnapshot<T>(ImmutableList<T>.Empty);
		return Disposable.Empty;
	}

	/// <inheritdoc />
	public IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current)
	{
		current = new ObservableCollectionSnapshot<T>(ImmutableList<T>.Empty);
		return Disposable.Empty;
	}

	/// <inheritdoc />
	public void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
	{
		current = new ObservableCollectionSnapshot<T>(ImmutableList<T>.Empty);
	}

	/// <inheritdoc />
	public void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current)
	{
		current = new ObservableCollectionSnapshot<T>(ImmutableList<T>.Empty);
	}

	#region Read (constant values)
	/// <inheritdoc />
	public int Count => 0;
	/// <inheritdoc />
	public bool IsReadOnly => true;
	/// <inheritdoc />
	public bool IsFixedSize => true;
	/// <inheritdoc />
	public bool IsSynchronized => true;
	/// <inheritdoc />
	public object SyncRoot { get; } = new object();

	/// <inheritdoc />
	public bool Contains(object? value) => false;
	/// <inheritdoc />
	public bool Contains(T item) => false;

	/// <inheritdoc />
	public int IndexOf(object? value) => -1;
	/// <inheritdoc />
	public int IndexOf(T item) => -1;

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator() { yield break; }
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex) { }
	/// <inheritdoc />
	public void CopyTo(Array array, int index) { }

	/// <inheritdoc />
	object? IList.this[int index]
	{
		get => throw OutOfRange();
		set => throw NotSupported();
	}

	/// <inheritdoc />
	public T this[int index]
	{
		get => throw OutOfRange();
		set => throw NotSupported();
	}
	#endregion

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

	// TODO: Uno
	//#region IExtensibleDIsposable
	//private readonly CompositeDisposable _extensions = new CompositeDisposable();

	///// <inheritdoc />
	//public IReadOnlyCollection<object> Extensions => _extensions.ToImmutableList();

	///// <inheritdoc />
	//public IDisposable RegisterExtension<TExtension>(TExtension extension)
	//	where TExtension : class, IDisposable
	//	=> _extensions.DisposableAdd(extension);

	///// <inheritdoc />
	//public void Dispose() => _extensions.Dispose();
	//#endregion

	private IndexOutOfRangeException OutOfRange()
		=> new IndexOutOfRangeException();

	private InvalidOperationException NotSupported([CallerMemberName] string? method = null)
		=> new InvalidOperationException($"Cannot '{method}' on a ReadOnlyCollection.");
}
