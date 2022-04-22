using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive.Utils;

/// <summary>
/// Cache of state-less properties
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public sealed class AttachedProperty
{
	/// <summary>
	/// Factory to create a <typeparamref name="T"/> given a <typeparamref name="TKey"/>.
	/// </summary>
	/// <typeparam name="TKey">Type of the delegate onto which the value has to be attached.</typeparam>
	/// <typeparam name="T">Type of the value to create.</typeparam>
	/// <param name="key">The key delegate onto which the value has to be attached.</param>
	/// <returns>The non null created value.</returns>
	/// <remarks><see cref="Func{TKey,T}"/> but which does not allow null as return value.</remarks>
	public delegate T Factory<in TKey, out T>(TKey key);

	/// <summary>
	/// Factory to create a <typeparamref name="T"/> given a <typeparamref name="TOwner"/> and a <typeparamref name="TKey"/>.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner onto which the value has to be attached.</typeparam>
	/// <typeparam name="TKey">Type of the key used to identify the attached value.</typeparam>
	/// <typeparam name="T">Type of the value to create.</typeparam>
	/// <param name="owner">The owner onto which the property is attached.</param>
	/// <param name="key">The key, usually a delegate.</param>
	/// <returns>The non null created value.</returns>
	/// <remarks><see cref="Func{TOwner, TKey,T}"/> but which does not allow null as return value.</remarks>
	public delegate T Factory<in TOwner, in TKey, out T>(TOwner owner, TKey key);

	/// <summary>
	/// Factory to create a <typeparamref name="T"/> given a <typeparamref name="TOwner"/>, a <typeparamref name="TKey"/> and a <typeparamref name="TState"/>.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner onto which the value has to be attached.</typeparam>
	/// <typeparam name="TKey">Type of the key used to identify the attached value.</typeparam>
	/// <typeparam name="TState">Type of the state to provide to the factory.</typeparam>
	/// <typeparam name="T">Type of the value to create.</typeparam>
	/// <param name="owner">The owner onto which the property is attached.</param>
	/// <param name="key">The key, usually a delegate.</param>
	/// <param name="state">The state to provide to the factory.</param>
	/// <returns>The non null created value.</returns>
	/// <remarks><see cref="Func{TOwner, TKey, TState, T}"/> but which does not allow null as return value.</remarks>
	public delegate T Factory<in TOwner, in TKey, in TState, out T>(TOwner owner, TKey key, TState state);

	private static readonly ConditionalWeakTable<object, AttachedProperty> _properties = new();

	// Note: We keep the target type in the cache key in order to allow the same method to be used in multiple operators
	//		 eg. Feed.Where(x => x > 0) / Feed.Select(x => x > 0)
	private readonly Dictionary<(object key, Type targetType), object> _values = new();

	/// <summary>
	/// Gets or creates an attached value.
	/// </summary>
	/// <typeparam name="TKey">Type of the delegate onto which the value has to be attached.</typeparam>
	/// <typeparam name="TValue">Type of the value to attach.</typeparam>
	/// <param name="key">The key delegate onto which the value has to be attached.</param>
	/// <param name="factory">The factory to use to create a value if none attached yet.</param>
	/// <returns>The attached value.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> or <see cref="Factory{TKey,T}"/> is null.</exception>
	/// <exception cref="InvalidOperationException">The factory returned null.</exception>
	public static TValue GetOrCreate<TKey, TValue>(TKey key, Factory<TKey, TValue> factory)
		where TKey : Delegate
	{
		key = key ?? throw new ArgumentNullException(nameof(key));
		factory = factory ?? throw new ArgumentNullException(nameof(factory));

		var owner = key.Target ?? key.Method; // Target is 'null' for static methods
		var cacheKey = (key, typeof(TValue));
		var values = _properties.GetOrCreateValue(owner)._values;
		if (values.TryGetValue(cacheKey, out var value))
		{
			return (TValue)value;
		}

		var newFeed = factory(key) ?? throw new InvalidOperationException("Factory result must not be null.");
		lock (values)
		{
			return values.TryGetValue(cacheKey, out value) 
				? (TValue)value 
				: (TValue)(values[cacheKey] = newFeed);
		}
	}

	/// <summary>
	/// Gets or creates an attached value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner onto which the value has to be attached.</typeparam>
	/// <typeparam name="TKey">Type of the key used to identify the attached value.</typeparam>
	/// <typeparam name="TValue">Type of the value to attach.</typeparam>
	/// <param name="owner">The owner onto which the property is attached.</param>
	/// <param name="key">The key, usually a delegate.</param>
	/// <param name="factory">The factory to use to create a value if none attached yet.</param>
	/// <returns>The attached value.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> or <see cref="Factory{TKey,T}"/> is null.</exception>
	/// <exception cref="InvalidOperationException">The factory returned null.</exception>
	public static TValue GetOrCreate<TOwner, TKey, TValue>(TOwner owner, TKey key, Factory<TOwner, TKey, TValue> factory)
		where TOwner : class
	{
		owner = owner ?? throw new ArgumentNullException(nameof(owner));
		key = key ?? throw new ArgumentNullException(nameof(key));
		factory = factory ?? throw new ArgumentNullException(nameof(factory));

		var cacheKey = (key as object, typeof(TValue));
		var values = _properties.GetOrCreateValue(owner)._values;
		if (values.TryGetValue(cacheKey, out var feed))
		{
			return (TValue)feed;
		}

		var newFeed = factory(owner, key) ?? throw new InvalidOperationException("Factory result must not be null.");
		lock (values)
		{
			return values.TryGetValue(cacheKey, out feed)
				? (TValue)feed
				: (TValue)(values[cacheKey] = newFeed);
		}
	}

	/// <summary>
	/// Gets or creates an attached value, providing a state into the factory.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner onto which the value has to be attached.</typeparam>
	/// <typeparam name="TKey">Type of the key used to identify the attached value.</typeparam>
	/// <typeparam name="TState">Type of the state to provide to the factory.</typeparam>
	/// <typeparam name="TValue">Type of the value to attach.</typeparam>
	/// <param name="owner">The owner onto which the property is attached.</param>
	/// <param name="key">The key, usually a delegate.</param>
	/// <param name="state">The state to provide to the factory.</param>
	/// <param name="factory">The factory to use to create a value if none attached yet.</param>
	/// <returns>The attached value.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> or <see cref="Factory{TKey,T}"/> is null.</exception>
	/// <exception cref="InvalidOperationException">The factory returned null.</exception>
	public static TValue GetOrCreate<TOwner, TKey, TState, TValue>(TOwner owner, TKey key, TState state, Factory<TOwner, TKey, TState, TValue> factory)
		where TOwner : class
	{
		owner = owner ?? throw new ArgumentNullException(nameof(owner));
		key = key ?? throw new ArgumentNullException(nameof(key));
		factory = factory ?? throw new ArgumentNullException(nameof(factory));

		var cacheKey = (key as object, typeof(TValue));
		var values = _properties.GetOrCreateValue(owner)._values;
		if (values.TryGetValue(cacheKey, out var feed))
		{
			return (TValue)feed;
		}

		var newFeed = factory(owner, key, state) ?? throw new InvalidOperationException("Factory result must not be null.");
		lock (values)
		{
			return values.TryGetValue(cacheKey, out feed)
				? (TValue)feed
				: (TValue)(values[cacheKey] = newFeed);
		}
	}
}
