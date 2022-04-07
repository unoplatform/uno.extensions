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
	public class CompositeList : IList
	{
		private readonly IList[] _inners;

		public CompositeList(params IList[] inners)
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
		public bool IsFixedSize => _inners.All(i => i.IsFixedSize);
		/// <inheritdoc />
		public bool IsReadOnly => _inners.Any(i => i.IsReadOnly);
		/// <inheritdoc />
		public bool IsSynchronized => _inners.All(i => i.IsSynchronized);
		/// <inheritdoc />
		public object SyncRoot { get; } = new object();

		/// <inheritdoc />
		public object this[int index]
		{
			get => GetInner(ref index)[index];
			set => GetInner(ref index)[index] = value;
		}

		/// <inheritdoc />
		public bool Contains(object item) => _inners.Any(i => i.Contains(item));

		/// <inheritdoc />
		public int IndexOf(object item)
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
		public int Add(object value) => _inners.Last().Add(value) + _inners.Take(_inners.Length - 1).Sum(i => i.Count);

		/// <inheritdoc />
		public void Insert(int index, object item) => GetInner(ref index).Insert(index, item);

		/// <inheritdoc />
		public void Remove(object item) => _inners.FirstOrDefault(i => i.Contains(item))?.Remove(item);

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

		/// <inheritdoc />
		public IEnumerator GetEnumerator() => new CompositeEnumerator(_inners);

		/// <inheritdoc />
		public void CopyTo(Array array, int index)
		{
			foreach (var inner in _inners)
			{
				((ICollection)inner).CopyTo(array, index);
				index += inner.Count;
			}
		}

		private IList GetInner(ref int index)
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
