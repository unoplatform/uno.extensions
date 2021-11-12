using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Cache of state-less properties
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public sealed class AttachedProperty
{
	// Func<T> but which does not allow null as return value
	public delegate T Factory<in TKey, out T>(TKey key);
	public delegate T Factory<in TOwner, in TKey, out T>(TOwner owner, TKey key);
	public delegate T Factory<in TOwner, in TKey, in TState, out T>(TOwner owner, TKey key, TState state);

	private static readonly ConditionalWeakTable<object, AttachedProperty> _properties = new();

	// Note: We keep the target type in the cache key in order to allow the same method to be used in multiple operators
	//		 eg. Feed.Where(x => x > 0) / Feed.Select(x => x > 0)
	private readonly Dictionary<(object key, Type targetType), object> _values = new();

	public static TValue GetOrCreate<TKey, TValue>(TKey key, Factory<TKey, TValue> factory)
		where TKey : Delegate
	{
		key = key ?? throw new ArgumentNullException(nameof(key));
		factory = factory ?? throw new ArgumentNullException(nameof(factory));

		var owner = key.Target ?? key.Method; // Target is 'null' for static methods
		var cacheKey = (key, typeof(TValue));
		var feeds = _properties.GetOrCreateValue(owner)._values;
		if (feeds.TryGetValue(cacheKey, out var feed))
		{
			return (TValue)feed;
		}

		var newFeed = factory(key) ?? throw new InvalidOperationException("Factory result must not be null.");
		lock (feeds)
		{
			return feeds.TryGetValue(cacheKey, out feed) 
				? (TValue)feed 
				: (TValue)(feeds[cacheKey] = newFeed);
		}
	}

	public static TValue GetOrCreate<TOwner, TKey, TValue>(TOwner owner, TKey key, Factory<TOwner, TKey, TValue> factory)
		where TOwner : class
	{
		owner = owner ?? throw new ArgumentNullException(nameof(owner));
		key = key ?? throw new ArgumentNullException(nameof(key));
		factory = factory ?? throw new ArgumentNullException(nameof(factory));

		var cacheKey = (key as object, typeof(TValue));
		var feeds = _properties.GetOrCreateValue(owner)._values;
		if (feeds.TryGetValue(cacheKey, out var feed))
		{
			return (TValue)feed;
		}

		var newFeed = factory(owner, key) ?? throw new InvalidOperationException("Factory result must not be null.");
		lock (feeds)
		{
			return feeds.TryGetValue(cacheKey, out feed)
				? (TValue)feed
				: (TValue)(feeds[cacheKey] = newFeed);
		}
	}

	public static TValue GetOrCreate<TOwner, TKey, TState, TValue>(TOwner owner, TKey key, TState state, Factory<TOwner, TKey, TState, TValue> factory)
		where TOwner : class
	{
		owner = owner ?? throw new ArgumentNullException(nameof(owner));
		key = key ?? throw new ArgumentNullException(nameof(key));
		factory = factory ?? throw new ArgumentNullException(nameof(factory));

		var cacheKey = (key as object, typeof(TValue));
		var feeds = _properties.GetOrCreateValue(owner)._values;
		if (feeds.TryGetValue(cacheKey, out var feed))
		{
			return (TValue)feed;
		}

		var newFeed = factory(owner, key, state) ?? throw new InvalidOperationException("Factory result must not be null.");
		lock (feeds)
		{
			return feeds.TryGetValue(cacheKey, out feed)
				? (TValue)feed
				: (TValue)(feeds[cacheKey] = newFeed);
		}
	}
}
