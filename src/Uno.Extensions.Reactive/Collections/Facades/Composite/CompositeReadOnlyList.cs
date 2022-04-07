using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Extensions;

namespace Umbrella.Feeds.Collections.Facades
{
	public class CompositeReadOnlyList : IList
	{
		private readonly IList[] _inners;

		public CompositeReadOnlyList(params IList[] inners)
		{
			if (inners?.Length == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(inners), "The inners must have at least one collection.");
			}

			_inners = inners;
		}

		private IList GetForIndex(ref int index)
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

		#region Collection
		/// <inheritdoc />
		public int Count => _inners.Sum(i => i.Count);
		/// <inheritdoc />
		public bool IsFixedSize => _inners.All(i => ((IList)i).IsFixedSize);

		/// <inheritdoc />
		public bool IsReadOnly { get; } = true;

		/// <inheritdoc />
		public bool IsSynchronized => _inners.All(i => ((ICollection)i).IsSynchronized);
		/// <inheritdoc />
		public object SyncRoot { get; } = new object();

		/// <inheritdoc />
		public object this[int index]
		{
			get => GetForIndex(ref index)[index];
			set => throw NotSupported();
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
		public IEnumerator GetEnumerator() => new CompositeEnumerator(_inners);

		/// <inheritdoc />
		public void CopyTo(Array array, int index)
		{
			foreach (var inner in _inners)
			{
				inner.CopyTo(array, index);
				index += inner.Count;
			}
		}
		#endregion

		#region Write operations (NotSupported)
		int IList.Add(object value) => throw NotSupported();
		void IList.Insert(int index, object value) => throw NotSupported();
		void IList.RemoveAt(int index) => throw NotSupported();
		void IList.Remove(object value) => throw NotSupported();
		void IList.Clear() => throw NotSupported();

		private NotSupportedException NotSupported([CallerMemberName] string method = null)
			=> new NotSupportedException($"{method} not supported on a read only list.");
		#endregion
	}
}
