using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Edition;

/// <summary>
/// Static methods to get an <see cref="IValueAccessor{TEntity,TValue}"/> from a <see cref="PropertySelector{TEntity,TValue}"/>
/// </summary>
public static class PropertySelectors
{
	private static readonly ConditionalWeakTable<Delegate, object> _cache = new();
	private static ImmutableDictionary<string, object> _accessors = ImmutableDictionary<string, object>.Empty;

	/// <summary>
	/// Gets the <see cref="IValueAccessor{TEntity,TValue}"/> that has been generated at compile time for the given from a <see cref="PropertySelector{TEntity,TValue}"/>
	/// </summary>
	/// <typeparam name="TEntity">Type of the owning entity.</typeparam>
	/// <typeparam name="TValue">Type of the selected property.</typeparam>
	/// <param name="selector">The property selector.</param>
	/// <param name="parameterName">In the "API method" signature, the name of the <paramref name="selector"/> parameter.</param>
	/// <param name="filePath">The value of the [CallerFilePath] argument used to invoke the "API method".</param>
	/// <param name="fileLine">The value of the [CallerFLineNumber] argument used to invoke the "API method".</param>
	/// <returns>A <see cref="IValueAccessor{TEntity,TValue}"/> that allows read-write access of the property path described by the <paramref name="selector"/>.</returns>
	[Pure]
	public static IValueAccessor<TEntity, TValue> Get<TEntity, TValue>(PropertySelector<TEntity, TValue> selector, string parameterName, string filePath, int fileLine)
		=> Get(selector, new(parameterName, filePath, fileLine));

	internal static IValueAccessor<TEntity, TValue> Get<TEntity, TValue>(PropertySelector<TEntity, TValue> selector, ResolutionKey key)
	{
		if (_cache.TryGetValue(selector, out var accessor))
		{
			return (IValueAccessor<TEntity, TValue>)accessor;
		}

		if (_accessors.TryGetValue(key.Value, out accessor))
		{
			_cache.Add(selector, accessor);
			return (IValueAccessor<TEntity, TValue>)accessor;
		}

		throw new InvalidOperationException(
			$"Cannot resolve value accessor for the selector '{selector}' with key '{key}'."
			+ "Make sure that code generation has not been disabled (or result has been linked out)"
			+ "and the initialization of the assembly has been completed properly.");
	}

	/// <summary>
	/// Registers an <see cref="IValueAccessor{TOwner,TValue}"/> for the given key.
	/// </summary>
	/// <remarks>This should not be used since the discovery is expected to be generated at compile time by the PropertySelectorsGenerationTool.</remarks>
	/// <typeparam name="TOwner">Type of the owning entity.</typeparam>
	/// <typeparam name="TValue">Type of the selected property.</typeparam>
	/// <param name="key">The unique identifier of the <paramref name="accessor"/>.</param>
	/// <param name="accessor">The accessor instance to register.</param>
	[EditorBrowsable(EditorBrowsableState.Never)] // Should be used only by code-gen
	public static void Register<TOwner, TValue>(string key, IValueAccessor<TOwner, TValue> accessor)
		=> ImmutableInterlocked.TryAdd(ref _accessors, key, accessor);
}
