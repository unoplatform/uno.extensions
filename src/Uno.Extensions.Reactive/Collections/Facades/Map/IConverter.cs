using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Uno.Extensions.Collections.Umbrella.Conversion
{
	/// <summary>
	/// Something that is capable to convert instances to and from two well-known types <typeparamref name="TFrom"/> and <typeparamref name="TTo"/>.
	/// </summary>
	/// <typeparam name="TFrom">Source instance type</typeparam>
	/// <typeparam name="TTo">Target instance type</typeparam>
	public interface IConverter<TFrom, TTo>
	{
		/// <summary>
		/// Converts an instance of <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.
		/// </summary>
		/// <param name="from">Source instance</param>
		/// <returns>The converted value</returns>
		TTo Convert(TFrom from);

		/// <summary>
		/// Converts back from instance of <typeparamref name="TTo"/> to <typeparamref name="TFrom"/>.
		/// </summary>
		/// <param name="to">Source instance</param>
		/// <returns>The converted value</returns>
		TFrom ConvertBack(TTo to);
	}
}
