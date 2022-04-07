using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;

namespace Umbrella.Feeds.Collections.Facades
{
	/// <summary>
	/// A sparse composite list
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CompositeList<T> : IList, IList<T>
	{
		private readonly IList<T>[] _inners;

		public CompositeList(params IList<T>[] inners)
		{
			if (inners?.Length == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(inners), "The inners must have at least one collection.");
			}

			_inners = inners;
		}

		#region Collection
		/// <inheritdoc />
		public int Count => _inners.Sum(i => i.Count);
		/// <inheritdoc />
		public bool IsFixedSize => _inners.All(i => ((IList)i).IsFixedSize);
		/// <inheritdoc />
		public bool IsReadOnly => _inners.Any(i => i.IsReadOnly);
		/// <inheritdoc />
		public bool IsSynchronized => _inners.All(i => ((ICollection)i).IsSynchronized);
		/// <inheritdoc />
		public object SyncRoot { get; } = new object();

		object IList.this[int index]
		{
			get => GetInner(ref index)[index];
			set => ((IList)GetInner(ref index))[index] = value;
		}
		/// <inheritdoc />
		public T this[int index]
		{
			get => GetInner(ref index)[index];
			set => GetInner(ref index)[index] = value;
		}

		/// <inheritdoc />
		public bool Contains(object value) => Contains((T)value);
		/// <inheritdoc />
		public bool Contains(T item) => _inners.Any(i => i.Contains(item));

		/// <inheritdoc />
		public int IndexOf(object value) => IndexOf((T)value);
		/// <inheritdoc />
		public int IndexOf(T item)
		{
			var count = 0;
			foreach (var inner in _inners)
			{
				var index = inner.IndexOf(item);
				if (index >= 0)
				{
					return count + index;
				}
				else
				{
					count += inner.Count;
				}
			}

			return -1;
		}

		/// <inheritdoc />
		public int Add(object value) => ((IList)_inners.Last()).Add(value) + _inners.Take(_inners.Length - 1).Sum(i => i.Count);

		/// <inheritdoc />
		public void Add(T item) => _inners.Last().Add(item);

		/// <inheritdoc />
		public void AddRange(IReadOnlyList<T> items) => _inners.Last().AddRange(items);

		/// <inheritdoc />
		public void Insert(int index, object value) => Insert(index, (T) value);
		/// <inheritdoc />
		public void Insert(int index, T item) => GetInner(ref index).Insert(index, item);

		/// <inheritdoc />
		public void Remove(object value) => Remove((T)value);
		/// <inheritdoc />
		public bool Remove(T item) => _inners.Any(i => i.Remove(item));

		/// <inheritdoc />
		public void RemoveAt(int index) => GetInner(ref index).RemoveAt(index);

		/// <inheritdoc />
		public void Clear()
		{
			foreach (var inner in _inners)
			{
				inner.Clear();
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => new CompositeEnumerator<T>(_inners);
		/// <inheritdoc />
		public IEnumerator<T> GetEnumerator() => new CompositeEnumerator<T>(_inners);

		/// <inheritdoc />
		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (var inner in _inners)
			{
				inner.CopyTo(array, arrayIndex);
				arrayIndex += inner.Count;
			}
		}
		/// <inheritdoc />
		public void CopyTo(Array array, int index)
		{
			foreach (var inner in _inners)
			{
				((ICollection)inner).CopyTo(array, index);
				index += inner.Count;
			}
		}

		private IList<T> GetInner(ref int index)
		{
			foreach (var inner in _inners)
			{
				var count = inner.Count;
				if (index >= count)
				{
					index -= count;
				}
				else
				{
					return inner;
				}
			}

			throw new ArgumentOutOfRangeException(nameof(index));
		}
		#endregion
	}
}
