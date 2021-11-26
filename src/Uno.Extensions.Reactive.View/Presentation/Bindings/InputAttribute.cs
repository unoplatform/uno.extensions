using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Indicates that the input should be considered as a complex type which should be de-normalized,
/// so each field can be edit independently through bindings.
/// <remarks>This is the default behavior for records, unless <see cref="ValueAttribute"/> is set on parameter</remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class InputAttribute : Attribute
{
	public InputAttribute(InputKind type)
	{
	}
}

[EditorBrowsable(EditorBrowsableState.Never)]
public enum InputKind
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
