using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Conversion;

/// <summary>
/// A thread safe <see cref="IConverter{TFrom, TTo}"/> which weakly cache the converted values, and coalesce the results using an <see cref="IEqualityComparer{TTo}"/>.
/// </summary>
/// <typeparam name="TFrom">The source value type</typeparam>
/// <typeparam name="TTo">The target value type</typeparam>
internal class OneWayMemoizedConverter<
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	TFrom,
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	TTo
> : ICachedConverter<TFrom, TTo>, IConverter<TFrom, TTo>
	where TFrom : class
	where TTo : class
{
	private readonly ConditionalWeakTable<TFrom, TTo> _convertionStore = new();
	private readonly ConditionalWeakTable<TTo, TFrom> _convertionBackStore = new();
	private ImmutableDictionary<int, ImmutableList<WeakReference<TTo>>> _convertionBackAlternativeStore = ImmutableDictionary<int, ImmutableList<WeakReference<TTo>>>.Empty;

	private readonly Func<TFrom, TTo> _convert;
	private readonly IEqualityComparer<TTo> _resultComparer;

	/// <param name="convert">Convert method to convert a <typeparamref name="TFrom" /> in a <typeparamref name="TTo"/>.</param>
	/// <param name="resultComparer">Comparer of <typeparamref name="TTo"/> which is used to find the original version of the provided <typeparamref name="TTo"/> in the back conversion.</param>
	public OneWayMemoizedConverter(Func<TFrom, TTo> convert, IEqualityComparer<TTo> resultComparer)
	{
		_convert = convert;
		_resultComparer = resultComparer;

		ConvertCore = _ConvertCore;
		ConvertBackCore = _ConvertBackCore;
	}

	/// <inheritdoc />
	public TTo Convert(TFrom from)
		=> _convertionStore.GetValue(from, ConvertCore);

	/// <inheritdoc />
	public TFrom ConvertBack(TTo to)
		=> _convertionBackStore.GetValue(to, ConvertBackCore);

	/// <inheritdoc />
	public void Init(TFrom value)
	{
		Convert(value);
	}

	/// <inheritdoc />
	public void Release(TFrom value)
	{
		// As every things is weak referenced, we don't have to do anything here
	}

	/// <inheritdoc />
	public void ReleaseAll()
	{
		// As every things is weak referenced, we don't have to do anything here
	}

	private readonly ConditionalWeakTable<TFrom, TTo>.CreateValueCallback ConvertCore;
	private TTo _ConvertCore(TFrom source)
	{
		var result = _convert(source);
		if (result == null)
		{
			throw new InvalidOperationException($"Converting '{source}' returned 'null' which is invalid for the memoized converter (Cannot be attached the original value for back conversion).");
		}

		_convertionBackStore.Add(result, source);

		// Makes the freshly converted value discoverable for back conversion
		ImmutableInterlocked.AddOrUpdate(ref _convertionBackAlternativeStore, _resultComparer.GetHashCode(result), AddResult, ScavengeAndAddResult);

		return result;

		ImmutableList<WeakReference<TTo>> AddResult(int _)
			=> ImmutableList.Create(new System.WeakReference<TTo>(result));

		ImmutableList<WeakReference<TTo>> ScavengeAndAddResult(int _, ImmutableList<WeakReference<TTo>> alternativeResults)
			=> alternativeResults.RemoveAll(w => !w.TryGetTarget(out var _)).Add(new WeakReference<TTo>(result));
	}

	private readonly ConditionalWeakTable<TTo, TFrom>.CreateValueCallback ConvertBackCore;
	private TFrom _ConvertBackCore(TTo source)
	{
		if (!_convertionBackAlternativeStore.TryGetValue(_resultComparer.GetHashCode(source), out var alternativesForHashCode))
		{
			throw new InvalidOperationException("This converter supports back conversion only for items that was previously converted, or for its alternative versions (KeyEquals).");
		}

		var (hasAlternativeSource, alternativeSource) = Find(alternativesForHashCode, source, _resultComparer);
		if (!hasAlternativeSource)
		{
			throw new InvalidOperationException("This converter supports back conversion only for items that was previously converted, or for its alternative versions (KeyEquals).");
		}

		if (alternativeSource is null || !_convertionBackStore.TryGetValue(alternativeSource, out var result))
		{
			throw new InvalidOperationException("Invalid state");
		}

		return result;
	}

	private static (bool hasResult, TTo? result) Find(ImmutableList<System.WeakReference<TTo>> items, TTo target, IEqualityComparer<TTo> comparer)
	{
		foreach (var weakItem in items)
		{
			if (weakItem.TryGetTarget(out var item)
				&& comparer.Equals(target, item))
			{
				return (true, item);
			}
		}

		return (false, default);
	}
}
