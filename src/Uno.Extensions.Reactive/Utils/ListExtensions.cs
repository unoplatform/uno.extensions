using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Uno.Extensions.Reactive.Utils;

internal static class ListExtensions
{
	public static int IndexOf(this IList list, object? value, IEqualityComparer? comparer)
		=> list.IndexOf(value, 0, comparer);

	public static int IndexOf(this IList list, object? value, int index, IEqualityComparer? comparer)
	{
		if (comparer is null)
		{
			if (index is 0)
			{
				return list.IndexOf(value);
			}

			switch (list)
			{
				case IImmutableList<object?> immutable:
					return immutable.IndexOf(value, index);
				case List<object?> impl:
					return impl.IndexOf(value, index);
				case Array array:
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
				case IImmutableList<object?> immutable:
					return immutable.IndexOf(value, index, immutable.Count - index, comparer.ToEqualityComparer<object?>());
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
}
