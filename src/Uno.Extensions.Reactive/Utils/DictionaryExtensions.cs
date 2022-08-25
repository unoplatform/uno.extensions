using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Utils;

internal static class DictionaryExtensions
{
	public static Dictionary<TKey, TValue> ToDictionaryWhereKey<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnly, Predicate<TKey> predicate)
	{
		var result = new Dictionary<TKey, TValue>(readOnly.Count);
		foreach (var kvp in readOnly)
		{
			if (predicate(kvp.Key))
			{
				result.Add(kvp.Key, kvp.Value);
			}
		}

		return result;
	}

	public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> readOnly)
	{
		if (readOnly is IDictionary<TKey, TValue> dic)
		{
			return new(dic);
		}
		else
		{
			return readOnly.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}
	}

	public static Dictionary<TKey, TValue> SetItems<TKey, TValue>(this Dictionary<TKey, TValue> target, IReadOnlyDictionary<TKey, TValue> source)
	{
		foreach (var item in source)
		{
			target[item.Key] = item.Value;
		}

		return target;
	}
}
