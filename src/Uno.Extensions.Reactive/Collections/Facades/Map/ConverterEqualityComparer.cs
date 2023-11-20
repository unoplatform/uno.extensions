using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Conversion;

namespace Uno.Extensions.Conversion;

/// <summary>
/// An <see cref="IEqualityComparer{TTo}"/> which convert data using and <see cref="IConverter{TFrom,TTo}"/> in order to compare them using another <see cref="IEqualityComparer{TFrom}"/>
/// </summary>
/// <typeparam name="TFrom">Source instance type</typeparam>
/// <typeparam name="TTo">Target instance type</typeparam>
internal class ConverterEqualityComparer<TFrom, TTo> : IEqualityComparer<TTo>
{
	private readonly IConverter<TFrom, TTo> _converter;
	private readonly IEqualityComparer<TFrom> _comparer;

	public ConverterEqualityComparer(IConverter<TFrom, TTo> converter, IEqualityComparer<TFrom> comparer)
	{
		_converter = converter;
		_comparer = comparer;
	}

	/// <inheritdoc />
	public bool Equals(TTo? left, TTo? right)
		=> (left, right) switch
		{
			(null, null) => true,
			(null, _) => false,
			(_, null) => false,
			_ => _comparer.Equals(_converter.ConvertBack(left), _converter.ConvertBack(right))
		};

	/// <inheritdoc />
	public int GetHashCode(TTo obj)
		=> _converter.ConvertBack(obj) is { } back ? _comparer.GetHashCode(back) : -1;
}
