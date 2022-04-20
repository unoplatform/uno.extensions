using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Uno.Extensions;

public readonly struct Option<T> : IOption, IEquatable<Option<T>>
{
	public static Option<T> Some(T value)
		=> new(OptionType.Some, value);

	public static Option<T> None()
		=> default;

	public static Option<T> Undefined()
		=> new(OptionType.Undefined);

	private readonly OptionType _type;
	private readonly T? _value;

	internal Option(OptionType type, T? value = default)
	{
		_type = type;
		_value = value;
	}

	public OptionType Type => _type;

	public bool IsUndefined()
		=> _type is OptionType.Undefined;

	public bool IsNone()
		=> _type is OptionType.None;

	public bool IsSome([NotNullWhen(true)] out T value)
	{
		value = _value!;
		return _type is OptionType.Some;
	}

	bool IOption.IsSome(out object? value)
	{
		value = _value;
		return _type is OptionType.Some;
	}

	public T? SomeOrDefault()
		=> _value;

	object? IOption.SomeOrDefault()
		=> _value;

	public T SomeOrDefault(T defaultValue)
		=> _type is OptionType.Some 
			? _value!
			: defaultValue;

	public static implicit operator Option<T>(T value)
		=> new(OptionType.Some, value);

	public static explicit operator T(Option<T> option)
	{
		if (option._type is not OptionType.Some)
		{
			throw new InvalidCastException($"Option is {option._type}, only Some can be cast to T.");
		}

		return option._value!;
	}

	public static explicit operator Option<object>(Option<T> option)
		=> new(option._type, option._value);

	public static explicit operator Option<T>(Option<object> option)
		=> new(option._type, option._value is T value ? value : default);

	/// <inheritdoc />
	public override int GetHashCode()
		=> Type switch
		{
			OptionType.Undefined => int.MinValue,
			OptionType.None => int.MaxValue,
			OptionType.Some when _value is not null => _value.GetHashCode(),
			_ => 0 // Some(default(T))
		};

	/// <inheritdoc />
	public bool Equals(Option<T> other)
		=> Equals(this, other);

	/// <inheritdoc />
	public override bool Equals(object? obj)
		=> obj is Option<T> other && Equals(this, other);

	internal static bool Equals(Option<T> x, Option<T> y)
		=> y.Type switch
		{
			OptionType.Undefined when x.IsUndefined() => true,
			OptionType.None when x.IsNone() => true,
			OptionType.Some when x.IsSome(out var xValue) => object.Equals(xValue, y._value),
			_ => false,
		};

	/// <inheritdoc />
	public override string ToString()
		=> Type switch
		{
			OptionType.Undefined => $"Undefined<{typeof(T).Name}>",
			OptionType.None => $"None<{typeof(T).Name}>",
			_ => $"Some({_value})",
		};
}
