using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Umbrella.Feeds.Conversion;
using Uno.Extensions.Collections.Umbrella.Conversion;

namespace Uno.Extensions.Collections.Umbrella.Extensions
{
	/// <summary>
	/// Extensions method over <see cref="IConverter{TFrom,TTo}"/>.
	/// </summary>
	public static class ConverterExtensions
	{
		/// <summary>
		/// Converts an object, which is assumed to be a <typeparamref name="TFrom"/> to a <typeparamref name="TTo"/>.
		/// </summary>
		/// <typeparam name="TFrom">Type of the source value</typeparam>
		/// <typeparam name="TTo">Type of the target value</typeparam>
		/// <param name="converter">The converter to use to convert the given value to the target type</param>
		/// <param name="from">The value to convert</param>
		/// <returns>The converted value</returns>
		public static TTo Convert<TFrom, TTo>(this IConverter<TFrom, TTo> converter, object from)
			=> converter.Convert((TFrom)from);

		/// <summary>
		/// Converts back an object, which is assumed to be a <typeparamref name="TTo"/> to a <typeparamref name="TFrom"/>.
		/// </summary>
		/// <typeparam name="TFrom">Type of the target value</typeparam>
		/// <typeparam name="TTo">Type of the source value</typeparam>
		/// <param name="converter">The converter to use to convert back the given value to the target type</param>
		/// <param name="to">The value to convert back</param>
		/// <returns>The converted value</returns>
		public static TFrom ConvertBack<TFrom, TTo>(this IConverter<TFrom, TTo> converter, object to)
			=> converter.ConvertBack((TTo)to);

		/// <summary>
		/// Gets a converter which inverse the conversion of the given converter.
		/// </summary>
		public static IConverter<TTo, TFrom> Inverse<TFrom, TTo>(this IConverter<TFrom, TTo> converter)
			=> InverseConverter.Get(converter);

		/// <summary>
		/// Helper to implements the <see cref="ICollection.CopyTo"/> method using a <see cref="IConverter{TFrom, TTo}"/>.
		/// </summary>
		public static void ArrayCopy<TFrom, TTo>(this IConverter<TFrom, TTo> converter, ICollection source, Array targetArray, int targetIndex)
		{
			var length = targetArray.Length;
			var sourceArray = new TFrom[length];
			source.CopyTo(sourceArray, 0);
			for (var i = 0; i < length; i++)
			{
				targetArray.SetValue(converter.Convert(sourceArray[i]), i + targetIndex);
			}
		}

		/// <summary>
		/// Helper to implements the <see cref="ICollection.CopyTo"/> method using a <see cref="IConverter{TFrom, TTo}"/>.
		/// </summary>
		public static void ArrayCopy<TFrom, TTo>(this IConverter<TFrom, TTo> converter, ICollection source, TTo[] targetArray, int targetIndex)
		{
			var length = targetArray.Length;
			var sourceArray = new TTo[length];
			source.CopyTo(sourceArray, 0);
			for (var i = 0; i < length; i++)
			{
				var item = sourceArray[i];
				if (item is not null)
				{
					targetArray[i + targetIndex] = converter.Convert(item);
				}
			}
		}

		/// <summary>
		/// Creates an equality comparer decorator over an <see cref="IConverter{TFrom,TTo}"/>. 
		/// </summary>
		/// <typeparam name="TFrom">Source instance type</typeparam>
		/// <typeparam name="TTo">Target instance type</typeparam>
		/// <param name="converter">The converter</param>
		/// <param name="comparer">The decoratee</param>
		/// <returns>An equality comparer which converts back the values using <paramref name="converter"/>, and then compare them using <paramref name="converter"/>.</returns>
		public static IEqualityComparer<TTo> ToEqualityComparer<TFrom, TTo>(this IConverter<TFrom, TTo> converter, IEqualityComparer<TFrom> comparer)
			=> new ConverterEqualityComparer<TFrom, TTo>(converter, comparer);

		/// <summary>
		/// Creates an equality comparer decorator over an <see cref="IConverter{TFrom,TTo}"/>. 
		/// </summary>
		/// <typeparam name="TFrom">Source instance type</typeparam>
		/// <typeparam name="TTo">Target instance type</typeparam>
		/// <param name="converter">The converter</param>
		/// <param name="comparer">The decoratee</param>
		/// <returns>An equality comparer which converts the values using <paramref name="converter"/>, and then compare them using <paramref name="converter"/>.</returns>
		public static IEqualityComparer<TFrom> ToEqualityComparer<TFrom, TTo>(this IConverter<TFrom, TTo> converter, IEqualityComparer<TTo> comparer)
			=> new ConverterEqualityComparer<TTo, TFrom>(converter.Inverse(), comparer);

		/// <summary>
		/// Creates a new converter which anonymize the convertion types
		/// </summary>
		/// <remarks>Null object won't be converted</remarks>
		public static IConverter<object?, object?> ToConverter<TFrom, TTo>(this IConverter<TFrom, TTo> converter)
			=> new ObjectConverter(
				o => o is null ? default : converter.Convert((TFrom)o),
				o => o is null ? default : converter.ConvertBack((TTo)o));

		private class ObjectConverter : IConverter<object?, object?>
		{
			private readonly Func<object?, object?> _convert;
			private readonly Func<object?, object?> _convertBack;

			public ObjectConverter(Func<object?, object?> convert, Func<object?, object?> convertBack)
			{
				_convert = convert;
				_convertBack = convertBack;
			}

			public object? Convert(object? from) => _convert(from);

			public object? ConvertBack(object? to) => _convertBack(to);
		}
	}
}
