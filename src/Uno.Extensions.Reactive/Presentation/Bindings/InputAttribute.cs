using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Enumeration of the possible modes for an input
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal enum InputKind
{
	/// <summary>
	/// Indicates that the input has to be considered as external which is going to be injected, so it won't be accessible through bindings.
	/// </summary>
	External,

	/// <summary>
	/// Indicates that the input should be de-normalized, so each property can be edited independently through bindings.
	/// </summary>
	/// <remarks>This is the default behavior for records.</remarks>
	Edit,

	/// <summary>
	/// Indicates that the input should be considered as a simple value. It can be get/set/ through bindings, but it won't be de-normalized. 
	/// </summary>
	/// <remarks>This is the default behavior except for records.</remarks>
	Value
}

/// <summary>
/// Flag the parameter of a constructor to be considered an input for bindings.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[EditorBrowsable(EditorBrowsableState.Never)]
internal class InputAttribute : Attribute
{
	/// <summary>
	/// Flag the parameter of a constructor to be considered an input for bindings.
	/// </summary>
	/// <param name="type">The type of input</param>
	public InputAttribute(InputKind type)
	{
	}
}
