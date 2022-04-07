using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nVentive.Umbrella.Conversion;
using nVentive.Umbrella.Extensions;

namespace nVentive.Umbrella.Collections
{
	/// <summary>
	/// An <see cref="IEnumerator{T}"/> which ensure dynamic conversion of item using an <see cref="IConverter{TFrom,TTo}"/>.
	/// </summary>
	/// <typeparam name="TFrom">Type of item of the inner enumerator</typeparam>
	/// <typeparam name="TTo">Type of the item exposed by this enumerator</typeparam>
	public sealed class MapEnumerator<TFrom, TTo> : IEnumerator<TTo>
	{
		private readonly IConverter<TFrom, TTo> _converter;
		private readonly IEnumerator _source;

		public MapEnumerator(IConverter<TFrom, TTo> converter, IEnumerator source)
		{
			_converter = converter;
			_source = source;
		}

		/// <inheritdoc />
		object IEnumerator.Current => Current!;

		public TTo Current { get; private set; } = default!;

		/// <inheritdoc />
		public bool MoveNext()
		{
			if (_source.MoveNext())
			{
				Current = _converter.Convert(_source.Current!);
				return true;
			}
			else
			{
				Current = default!;
				return false;
			}
		}

		/// <inheritdoc />
		public void Reset()
		{
			_source.Reset();
			Current = default!;
		}

		/// <inheritdoc />
		public void Dispose() => (_source as IDisposable)?.Dispose();
	}
}
