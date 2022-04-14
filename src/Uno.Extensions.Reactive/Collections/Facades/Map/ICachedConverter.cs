using System;
using System.Linq;

namespace Uno.Extensions.Collections.Umbrella.Conversion
{
	/// <summary>
	/// An enhanced <see cref="IConverter{TFrom, TTo}"/> which cache the converted values.
	/// </summary>
	public interface ICachedConverter<TFrom, TTo> : IConverter<TFrom, TTo>
	{
		/// <summary>
		/// Initialize the cached value of an item.
		/// </summary>
		/// <param name="value">Value to prepoluate cache for</param>
		void Init(TFrom value);

		/// <summary>
		/// Release the convertion cached for a given value.
		/// <remarks>
		/// This does not means that the <see cref="Convert"/> will never be requested again for this value. 
		/// It's only notifying that the given value is removed from the owner of this converter.
		/// </remarks>
		/// </summary>
		void Release(TFrom value);

		/// <summary>
		/// Release all the convertions cached by this converter.
		/// <remarks>
		/// This does not means that the <see cref="Convert"/> will never be requested again for those values. 
		/// It's only notifying that the previously initialized values are removed from the owner of this converter.
		/// </remarks>
		/// </summary>
		void ReleaseAll();
	}
}