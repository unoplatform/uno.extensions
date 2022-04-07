using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Reactive.Utils;

internal static class ListExtensions
{
	public static int IndexOf(this IList list, object? value, IEqualityComparer? comparer)
	{
		return -1;
	}

	public static int IndexOf(this IList list, object? value, int startIndex, IEqualityComparer? comparer)
	{
		//if (comparer is null && startIndex is 0)
		//{
		//	return list.IndexOf(value);
		//}

		//var def = new List<object>();

		//def.IndexOf()

		//if ()=

		//ArrayList abc = default!;

		//abc.Inde

		//for (var i = startIndex; i < list.Count; i++)
		//{
		//	if (comparer.Equals(list[i], value))
		//	{
		//		return i;
		//	}
		//}

		return -1;
	}

	public static int IndexOf<T>(this IList<T> list, object? value, int startIndex, IEqualityComparer<T>? comparer)
	{
		return -1;
	}
}
