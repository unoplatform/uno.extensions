using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Uno.Extensions;

/// <summary>
/// A wrapper over a optional value that describes its absence or presence.
/// </summary>
/// <typeparam name="T">The type of the value</typeparam>
public readonly struct Option<T> : IOption, IEquatable<Option<T>>
{
	/// <summary>
	/// Creates a new optional value that is flagged as present.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The optional value.</returns>
	public static Option<T> Some(T value)
		=> new(OptionType.Some, value);

	/// <summary>
	/// Creates a new optional value that is flagged as absent.
	/// </summary>
	/// <returns>The optional value.</returns>
	public static Option<T> None()
		=> default;

	/// <summary>
	/// Creates a new optional value that is flagged as neither present nor absent.
	/// </summary>
	/// <returns>The optional value.</returns>
	public static Option<T> Undefined()
		=> new(OptionType.Undefined);

	private readonly OptionType _type;
	private readonly T? _value;

	internal Option(OptionType type, T? value = default)
	{
		_type = type;
		_value = value;
	}

	/// <summary>
	/// Gets the type of this optional value.
	/// </summary>
	public OptionType Type => _type;

	/// <summary>
	/// Gets a boolean that indicates if the <see cref="Type"/> of this optional is <see cref="OptionType.Undefined"/>.
	/// </summary>
	public bool IsUndefined()
		=> _type is OptionType.Undefined;

	/// <summary>
	/// Gets a boolean that indicates if the <see cref="Type"/> of this optional is <see cref="OptionType.None"/>.
	/// </summary>
	public bool IsNone()
		=> _type is OptionType.None;

	/// <summary>
	/// Gets a boolean that indicates if the <see cref="Type"/> of this optional is <see cref="OptionType.Some"/>.
	/// </summary>
	/// <param name="value">The value is any.</param>
	public bool IsSome([NotNullWhen(true)] out T value)
	{
		value = _value!;
		return _type is OptionType.Some;
	}

	/// <inheritdoc />
	bool IOption.IsSome(out object? value)
	{
		value = _value;
		return _type is OptionType.Some;
	}

	/// <summary>
	/// Gets the value if <see cref="Type"/> is <seealso cref="OptionType.Some"/>, other returns the default(<typeparamref name="T"/>).
	/// </summary>
	/// <returns>The value if Some, default(T) otherwise.</returns>
	public T? SomeOrDefault()
		=> _value;

	/// <inheritdoc />
	object? IOption.SomeOrDefault()
		=> _value;

	/// <summary>
	/// Gets the value if <see cref="Type"/> is <seealso cref="OptionType.Some"/>, other returns the <paramref name="defaultValue"/>.
	/// </summary>
	/// <param name="defaultValue">The default value to return is this optional value is not present.</param>
	/// <returns>The value if Some, <paramref name="defaultValue"/> otherwise.</returns>
	public T SomeOrDefault(T defaultValue)
		=> _type is OptionType.Some 
			? _value!
			: defaultValue;

	/// <summary>
	/// Creates a new optional value that is flagged as present.
	/// </summary>
	/// <param name="value">The value.</param>
	public static implicit operator Option<T>(T value)
		=> new(OptionType.Some, value);

	/// <summary>
	/// Gets the value of an option value if its <see cref="Type"/> is <seealso cref="OptionType.Some"/>
	/// </summary>
	/// <param name="option">The optional value.</param>
	/// <exception cref="InvalidCastException">If the <see cref="Type"/> is not <seealso cref="OptionType.Some"/>.</exception>
	public static explicit operator T(Option<T> option)
	{
		if (option._type is not OptionType.Some)
		{
			throw new InvalidCastException($"Option is {option._type}, only Some can be cast to T.");
		}

		return option._value!;
	}

	/// <summary>
	/// Changes the type of an optional value.
	/// </summary>
	/// <param name="option">The optional value.</param>
	public static explicit operator Option<object>(Option<T> option)
		=> new(option._type, option._value);

	/// <summary>
	/// Changes the type of an optional value.
	/// </summary>
	/// <param name="option">The optional value.</param>
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
			_ when _value is null => "Some(--null--)",
			_ when _value is string str => $"Some({str})",
			_ when _value is IEnumerable enumerable => $"Some({string.Join(",", enumerable.Cast<object>())})",
			_ => $"Some({_value})",
		};
}
