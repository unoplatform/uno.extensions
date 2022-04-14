using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace Uno.Extensions.Reactive.Utils;

internal static class ListExtensions
{
	public static int IndexOf(this IList list, object? value, IEqualityComparer? comparer)
		=> list.IndexOf(value, 0, comparer);

	public static int IndexOf(this IList list, object? value, int index, IEqualityComparer? comparer)
		=> list.IndexOf(value, index, list.Count - index, comparer);

	public static int IndexOf(this IList list, object? value, int index, int count, IEqualityComparer? comparer)
	{
		if (comparer is null)
		{
			if (index is 0 && count == list.Count)
			{
				return list.IndexOf(value);
			}

			switch (list)
			{
				case IImmutableList<object?> immutable:
					return immutable.IndexOf(value, index, count);
				case List<object?> impl:
					return impl.IndexOf(value, index, count);
				case Array array:
					return Array.IndexOf(array, value, index, count);
				default:
				{
					for (var i = index; i < index + count; i++)
					{
						if (object.Equals(list[i], value))
						{
							return i;
						}
					}

					return -1;
				}
			}
		}
		else
		{
			switch (list)
			{
				case IImmutableList<object?> immutable:
					return immutable.IndexOf(value, index, count, comparer.ToEqualityComparer<object?>());
				default:
				{
					for (var i = index; i < index + count; i++)
					{
						if (comparer.Equals(list[i], value))
						{
							return i;
						}
					}
					return -1;
				}
			}
		}
	}

	public static int IndexOf<T>(this IList<T> list, T value, IEqualityComparer<T>? comparer)
		=> list.IndexOf(value, 0, comparer);

	public static int IndexOf<T>(this IList<T> list, T value, int index, IEqualityComparer<T>? comparer)
	{
		if (comparer is null)
		{
			if (index is 0)
			{
				return list.IndexOf(value);
			}

			switch (list)
			{
				case IImmutableList<T> immutable:
					return immutable.IndexOf(value, index);
				case List<T> impl:
					return impl.IndexOf(value, index);
				case T[] array:
					return Array.IndexOf(array, value, index);
				default:
				{
					for (var i = index; i < list.Count; i++)
					{
						if (object.Equals(list[i], value))
						{
							return i;
						}
					}

					return -1;
				}
			}
		}
		else
		{
			switch (list)
			{
				case IImmutableList<T> immutable:
					return immutable.IndexOf(value, index, immutable.Count - index, comparer);
				default:
				{
					for (var i = index; i < list.Count; i++)
					{
						if (comparer.Equals(list[i], value))
						{
							return i;
						}
					}
					return -1;
				}
			}
		}
	}

	public static IList Slice(this IList list, int index, int count)
		=> new SubList(list, index, count);

	public class SubList : IList
	{
		private readonly IList _source;
		private readonly int _startIndex;

		public SubList(IList source, int startIndex, int count)
		{
			Count = count;
			_source = source;
			_startIndex = startIndex;
		}

		/// <inheritdoc />
		public int Count { get; }

		/// <inheritdoc />
		public bool IsSynchronized => _source.IsSynchronized;

		/// <inheritdoc />
		public object SyncRoot => _source.SyncRoot;

		/// <inheritdoc />
		public bool IsFixedSize => true;

		/// <inheritdoc />
		public bool IsReadOnly => true;

		/// <inheritdoc />
		public object this[int index]
		{
			get => _source[index];
			set => throw NotSupported();
		}

		/// <inheritdoc />
		public int Add(object value)
			=> throw NotSupported();

		/// <inheritdoc />
		public void Clear()
			=> throw NotSupported();

		/// <inheritdoc />
		public bool Contains(object value)
			=> IndexOf(value) >= 0;

		/// <inheritdoc />
		public int IndexOf(object value)
			=> _source.IndexOf(value, _startIndex, Count, null);

		/// <inheritdoc />
		public void Insert(int index, object value)
			=> throw NotSupported();

		/// <inheritdoc />
		public void Remove(object value)
			=> throw NotSupported();

		/// <inheritdoc />
		public void RemoveAt(int index)
			=> throw NotSupported();

		/// <inheritdoc />
		public IEnumerator GetEnumerator()
			=> new SubEnumerator(_source.GetEnumerator(), _startIndex, Count);

		/// <inheritdoc />
		public void CopyTo(Array array, int index)
		{
			var tmp = new object[_source.Count];
			_source.CopyTo(tmp, 0);
			Array.Copy(tmp, _startIndex, array, index, Count);
		}

		private NotSupportedException NotSupported([CallerMemberName] string? name = null)
			=> new(name + " is not supported on a ReadOnly list");
	}

	public class SubEnumerator : IEnumerator
	{
		private readonly IEnumerator _inner;
		private readonly int _start;
		private readonly int _end;

		private int _nextIndex;

		public SubEnumerator(IEnumerator inner, int index, int count)
		{
			_inner = inner;
			_start = index;
			_end = index + count - 1;
		}

		/// <inheritdoc />
		public object Current => _inner.Current;

		/// <inheritdoc />
		public bool MoveNext()
		{
			while (_nextIndex < _start && _inner.MoveNext())
			{
				_nextIndex++;
			}

			if (_nextIndex < _start || _nextIndex > _end)
			{
				return false;
			}

			if (MoveNext())
			{
				_nextIndex++;
				return true;
			}
			else
			{
				_nextIndex = int.MaxValue;
				return false;
			}
		}

		/// <inheritdoc />
		public void Reset()
		{
			_inner.Reset();
			_nextIndex = 0;
		}
	}
}
