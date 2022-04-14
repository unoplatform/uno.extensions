using System;
using System.Linq;

namespace Uno.Extensions.Collections.Umbrella.Conversion
{
	/// <summary>
	/// A converter which inverse the target and the source
	/// </summary>
	public static class InverseConverter
	{
		/// <summary>
		/// Get the inverse converter of a <see cref="IConverter{TFrom,TTo}"/>
		/// </summary>
		public static IConverter<TTo, TFrom> Get<TFrom, TTo>(IConverter<TFrom, TTo> converter)
		{
			return converter is Converter<TFrom, TTo> inverse
				? inverse.Original
				: new Converter<TTo, TFrom>(converter);
		}

		private class Converter<TFrom, TTo> : IConverter<TFrom, TTo>
		{
			public IConverter<TTo, TFrom> Original { get; }

			public Converter(IConverter<TTo, TFrom> original)
			{
				Original = original;
			}

			public TTo Convert(TFrom from) => Original.ConvertBack(from);
			public TFrom ConvertBack(TTo to) => Original.Convert(to);
		}
	}
}