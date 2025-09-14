using System.Collections;

namespace Uno.Extensions;
/// <summary>
/// Extension methods for the <see cref="IDictionary"/>
/// </summary>
public static class IDictionaryExtensions
{
	/// <summary>
	/// Adds or replaces the element to the <see cref="IDictionary"/> instance.
	/// </summary>
	/// <typeparam name="TKey"> The type of the key</typeparam>
	/// <typeparam name="TValue">The type of the value</typeparam>
	/// <param name="dictionary">The dictionary to add to.</param>
	/// <param name="key">The key parameter.</param>
	/// <param name="value">The value to add.</param>
	/// <returns>The previous value or its default, possibily <see langword="null"/> value.</returns>
	public static TValue? AddOrReplace<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
	{
		if (!dictionary.TryAdd(key, value))
		{
			var oldValue = dictionary[key];
			dictionary[key] = value;
			return oldValue;
		}
		return default(TValue);
	}

}
