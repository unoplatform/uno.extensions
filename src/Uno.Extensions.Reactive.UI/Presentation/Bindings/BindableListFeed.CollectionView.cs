using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Uno.Extensions.Reactive.Bindings;

partial class BindableListFeed<T> : ICollectionView, INotifyCollectionChanged
{
	/// <inheritdoc />
	public IEnumerator<object> GetEnumerator()
		=> _items.GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> ((IEnumerable)_items).GetEnumerator();

	/// <inheritdoc />
	public void Add(object item)
		=> _items.Add(item);

	/// <inheritdoc />
	public void Clear()
		=> _items.Clear();

	/// <inheritdoc />
	public bool Contains(object item)
		=> _items.Contains(item);

	/// <inheritdoc />
	public void CopyTo(object[] array, int arrayIndex)
		=> _items.CopyTo(array, arrayIndex);

	/// <inheritdoc />
	public bool Remove(object item)
		=> _items.Remove(item);

	/// <inheritdoc />
	public int Count => _items.Count;

	/// <inheritdoc />
	public bool IsReadOnly => _items.IsReadOnly;

	/// <inheritdoc />
	public int IndexOf(object item)
		=> _items.IndexOf(item);

	/// <inheritdoc />
	public void Insert(int index, object item)
		=> _items.Insert(index, item);

	/// <inheritdoc />
	public void RemoveAt(int index)
		=> _items.RemoveAt(index);

	/// <inheritdoc />
	public object? this[int index]
	{
		get => _items[index];
		set => _items[index] = value;
	}

	/// <inheritdoc />
	public bool MoveCurrentTo(object item)
		=> _items.MoveCurrentTo(item);

	/// <inheritdoc />
	public bool MoveCurrentToPosition(int index)
		=> _items.MoveCurrentToPosition(index);

	/// <inheritdoc />
	public bool MoveCurrentToFirst()
		=> _items.MoveCurrentToFirst();

	/// <inheritdoc />
	public bool MoveCurrentToLast()
		=> _items.MoveCurrentToLast();

	/// <inheritdoc />
	public bool MoveCurrentToNext()
		=> _items.MoveCurrentToNext();

	/// <inheritdoc />
	public bool MoveCurrentToPrevious()
		=> _items.MoveCurrentToPrevious();

	/// <inheritdoc />
	public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		=> _items.LoadMoreItemsAsync(count);

	/// <inheritdoc />
	public IObservableVector<object> CollectionGroups => _items.CollectionGroups;

	/// <inheritdoc />
	public object? CurrentItem => _items.CurrentItem;

	/// <inheritdoc />
	public int CurrentPosition => _items.CurrentPosition;

	/// <inheritdoc />
	public bool HasMoreItems => _items.HasMoreItems;

	/// <inheritdoc />
	public bool IsCurrentAfterLast => _items.IsCurrentAfterLast;

	/// <inheritdoc />
	public bool IsCurrentBeforeFirst => _items.IsCurrentBeforeFirst;

	/// <inheritdoc />
	public event VectorChangedEventHandler<object?>? VectorChanged
	{
		add => _items.AddVectorChangedHandler(value);
		remove => _items.RemoveVectorChangedHandler(value);
	}

	/// <inheritdoc />
	public event EventHandler<object>? CurrentChanged
	{
		add => _items.AddCurrentChangedHandler(value);
		remove => _items.RemoveCurrentChangedHandler(value);
	}

	/// <inheritdoc />
	public event CurrentChangingEventHandler? CurrentChanging
	{
		add => _items.AddCurrentChangingHandler(value);
		remove => _items.RemoveCurrentChangingHandler(value);
	}

	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler? CollectionChanged
	{
		add => _items.CollectionChanged += value;
		remove => _items.CollectionChanged -= value;
	}
}
