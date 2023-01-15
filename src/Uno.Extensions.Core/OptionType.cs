using System;
using System.Linq;

namespace Uno.Extensions;

/// <summary>
/// Defines the possible types of an <see cref="Option{T}"/>
/// </summary>
public enum OptionType : short
{
	/// <summary>
	/// Indicates the absence of a value
	/// </summary>
	None = 0, // The default so default(Option<T>) == Option<T>.None

	/// <summary>
	/// Indicates the presence of a value
	/// </summary>
	Some = 1,

	/// <summary>
	/// Indicates that incertitude about the presence or the absence of a value.
	/// </summary>
	Undefined = -1,
}
