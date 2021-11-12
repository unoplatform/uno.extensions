using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public readonly record struct MessageAxisValue
{
	public MessageAxisValue(object value)
	{
		IsSet = true;
		Value = value;
	}

	public static MessageAxisValue Unset => default;

	public bool IsSet { get; }

	public object? Value { get; }

	public void Deconstruct(out bool isSet, out object? value)
	{
		isSet = IsSet;
		value = Value;
	}
}
