using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A raw value of <see cref="MessageAxis"/>.
/// </summary>
public readonly record struct MessageAxisValue
{
	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="value">The encapsulated value.</param>
	public MessageAxisValue(object value)
	{
		IsSet = true;
		Value = value;
	}

	/// <summary>
	/// An empty value which indicates that the requested axis is not set.
	/// </summary>
	public static MessageAxisValue Unset => default;

	/// <summary>
	/// Indicates if the <see cref="Value"/> is set or not.
	/// </summary>
	public bool IsSet { get; }

	/// <summary>
	/// The metadata value.
	/// </summary>
	public object? Value { get; }

	/// <summary>
	/// Deconstructs this raw value.
	/// </summary>
	/// <param name="isSet"><see cref="IsSet"/>.</param>
	/// <param name="value"><see cref="Value"/>.</param>
	public void Deconstruct(out bool isSet, out object? value)
	{
		isSet = IsSet;
		value = Value;
	}

	/// <inheritdoc />
	public override string? ToString()
		=> (IsSet, Value) switch
		{
			(false, _) => "--unset--",
			(true, null) => "--null--",
			(_, var v) => v.ToString()
		};
}
