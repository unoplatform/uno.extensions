using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Conversion;

/// <summary>
/// A thread safe <see cref="IConverter{TFrom, TTo}"/> which weakly cache the converted values.
/// <remarks>It's safe to assume that only one instance of <typeparamref name="TTo"/> will be created per <typeparamref name="TFrom"/>.</remarks>
/// </summary>
/// <typeparam name="TFrom">The source value type</typeparam>
/// <typeparam name="TTo">The target value type</typeparam>
internal class LockedMemoizedConverter<
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	TFrom,
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	TTo
> : ICachedConverter<TFrom, TTo>, IConverter<TFrom, TTo>
	where TFrom : class
	where TTo : class
{
	private readonly object _convertionStoresGate = new object();
	private readonly ConditionalWeakTable<TFrom, TTo> _convertionStore = new();
	private readonly ConditionalWeakTable<TTo, TFrom> _convertionBackStore = new();

	private readonly Func<TFrom, TTo> _convert;
	private readonly Func<TTo, TFrom> _convertBack;

	public LockedMemoizedConverter(Func<TFrom, TTo> convert, Func<TTo, TFrom> convertBack)
	{
		_convert = convert;
		_convertBack = convertBack;
	}

	/// <inheritdoc />
	[return:NotNullIfNotNull("from")]
	public TTo? Convert(TFrom? from)
	{
		if (from is null) // Might happen if TFrom is nullable
		{
			return default!;
		}

		lock (_convertionStoresGate)
		{
			return _convertionStore.GetValue(from, ConvertCore);
		}
	}

	/// <inheritdoc />
	[return: NotNullIfNotNull("to")]
	public TFrom? ConvertBack(TTo? to)
	{
		if (to is null) // Even if illegal, try to avoid crash.
		{
			return default!;
		}

		lock (_convertionStoresGate)
		{
			return _convertionBackStore.GetValue(to, ConvertBackCore);
		}
	}

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

	private TTo ConvertCore(TFrom source)
	{
		var result = _convert(source);
		if (result == null)
		{
			throw new InvalidOperationException("The conversion result must not be null.");
		}

		_convertionBackStore.Add(result, source);
		return result;
	}

	private TFrom ConvertBackCore(TTo source)
	{
		var result = _convertBack(source);
		if (result == null)
		{
			throw new InvalidOperationException("The back conversion result must not be null.");
		}

		_convertionStore.Add(result, source);
		return result;
	}
}
