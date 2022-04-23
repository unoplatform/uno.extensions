using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Uno.Extensions.Collections.Facades.Slice;

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
		=> new SliceList(list, index, count);
}
