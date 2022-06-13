using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Umbrella.Feeds.Tests._TestUtils;
using Uno.Equality;
using Uno.Extensions.Collections;

namespace Umbrella.Presentation.Feeds.Tests.Collections._TestUtils
{
	public sealed class MyGroup : IObservableGroup<MyKey, MyItem>, IKeyEquatable<MyGroup>
	{
		private readonly IObservableCollection<MyItem> _inner;

		public MyGroup(MyKey key, IObservableCollection<MyItem> inner)
		{
			Key = key;
			_inner = inner;
		}

		object IObservableGroup.Key => Key;
		public MyKey Key { get; }

		public int GetKeyHashCode() => ((IKeyEquatable)Key).GetKeyHashCode();
		bool IKeyEquatable.KeyEquals(object obj) => obj is MyGroup other && ((IKeyEquatable)Key).KeyEquals(other.Key);
		bool IKeyEquatable<MyGroup>.KeyEquals(MyGroup other) => ((IKeyEquatable)Key).KeyEquals(other.Key);
		public bool KeyEquals(IObservableGroup<MyKey, MyItem> other) => ((IKeyEquatable)Key).KeyEquals(((IObservableGroup)other).Key);

		public override int GetHashCode() => Key.GetHashCode();
		public override bool Equals(object obj) => obj is MyGroup other && Key.Equals(other.Key) && this.SequenceEqual(other, EqualityComparer<MyItem>.Default);


		#region IObservableGroup<MyItem>
		public IEnumerator<MyItem> GetEnumerator()
		{
			return _inner.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_inner).GetEnumerator();
		}

		public void CopyTo(Array array, int index)
		{
			_inner.CopyTo(array, index);
		}

		int IObservableCollection<MyItem>.Count
		{
			get { return _inner.Count; }
		}

		bool IObservableCollection<MyItem>.IsReadOnly
		{
			get { return _inner.IsReadOnly; }
		}

		public bool Remove(MyItem item)
		{
			return _inner.Remove(item);
		}

		public void RemoveCollectionChangedHandler(ISchedulerInfo context, Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<MyItem> current)
		{
			_inner.RemoveCollectionChangedHandler(context, callback, out current);
		}

		int ICollection<MyItem>.Count
		{
			get { return ((ICollection<MyItem>)_inner).Count; }
		}

		bool ICollection<MyItem>.IsReadOnly
		{
			get { return ((ICollection<MyItem>)_inner).IsReadOnly; }
		}

		int ICollection.Count
		{
			get { return ((ICollection)_inner).Count; }
		}

		public object SyncRoot
		{
			get { return _inner.SyncRoot; }
		}

		public bool IsSynchronized
		{
			get { return _inner.IsSynchronized; }
		}

		public int Add(object value)
		{
			return _inner.Add(value);
		}

		public bool Contains(object value)
		{
			return _inner.Contains(value);
		}

		void IObservableCollection<MyItem>.Clear()
		{
			_inner.Clear();
		}

		void IObservableCollection<MyItem>.RemoveAt(int index)
		{
			_inner.RemoveAt(index);
		}

		public void Add(MyItem item)
		{
			_inner.Add(item);
		}

		void ICollection<MyItem>.Clear()
		{
			_inner.Clear();
		}

		public bool Contains(MyItem item)
		{
			return _inner.Contains(item);
		}

		public void CopyTo(MyItem[] array, int arrayIndex)
		{
			_inner.CopyTo(array, arrayIndex);
		}

		void IList.Clear()
		{
			((IList)_inner).Clear();
		}

		public int IndexOf(object value)
		{
			return _inner.IndexOf(value);
		}

		public void Insert(int index, object value)
		{
			_inner.Insert(index, value);
		}

		bool IObservableCollection.Remove(object item)
		{
			return _inner.Remove(item);
		}

		IObservableCollectionSnapshot IObservableCollection.CurrentItems
		{
			get { return ((IObservableCollection)_inner).CurrentItems; }
		}

		public void AddRange(IReadOnlyList<MyItem> items)
		{
			_inner.AddRange(items);
		}

		public void ReplaceRange(int index, int count, IReadOnlyList<MyItem> newItems)
		{
			_inner.ReplaceRange(index, count, newItems);
		}

		public IDisposable AddCollectionChangedHandler(ISchedulerInfo context, Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<MyItem> current)
		{
			return _inner.AddCollectionChangedHandler(context, callback, out current);
		}

		public IObservableCollectionSnapshot<MyItem> CurrentItems
		{
			get { return _inner.CurrentItems; }
		}

		public IDisposable AddCollectionChangedHandler(ISchedulerInfo context, Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
		{
			return _inner.AddCollectionChangedHandler(context, callback, out current);
		}

		public void RemoveCollectionChangedHandler(ISchedulerInfo context, Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
		{
			_inner.RemoveCollectionChangedHandler(context, callback, out current);
		}

		void IList.Remove(object value)
		{
			((IList)_inner).Remove(value);
		}

		public int IndexOf(MyItem item)
		{
			return _inner.IndexOf(item);
		}

		public void Insert(int index, MyItem item)
		{
			_inner.Insert(index, item);
		}

		void IList<MyItem>.RemoveAt(int index)
		{
			_inner.RemoveAt(index);
		}

		public MyItem this[int index]
		{
			get { return _inner[index]; }
			set { _inner[index] = value; }
		}

		MyItem IList<MyItem>.this[int index]
		{
			get { return ((IList<MyItem>)_inner)[index]; }
			set { ((IList<MyItem>)_inner)[index] = value; }
		}

		void IList.RemoveAt(int index)
		{
			((IList)_inner).RemoveAt(index);
		}

		object IList.this[int index]
		{
			get { return ((IList)_inner)[index]; }
			set { ((IList)_inner)[index] = value; }
		}

		bool IList.IsReadOnly
		{
			get { return ((IList)_inner).IsReadOnly; }
		}

		public bool IsFixedSize
		{
			get { return _inner.IsFixedSize; }
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add { _inner.CollectionChanged += value; }
			remove { _inner.CollectionChanged -= value; }
		}

		public event NotifyCollectionReadEventHandler CollectionRead
		{
			add { _inner.CollectionRead += value; }
			remove { _inner.CollectionRead -= value; }
		}

		public void Dispose()
		{
			_inner.Dispose();
		}

		public IReadOnlyCollection<object> Extensions
		{
			get { return _inner.Extensions; }
		}

		public IDisposable RegisterExtension<T>(T extension)
			where T : class, IDisposable
		{
			return _inner.RegisterExtension(extension);
		}
		#endregion
	}
}
