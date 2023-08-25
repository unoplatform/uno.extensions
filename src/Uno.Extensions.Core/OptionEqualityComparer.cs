#pragma warning disable CS1591 // XML Doc, will be moved elsewhere

using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions;

public sealed class OptionEqualityComparer<T> : IEqualityComparer<Option<T>>
{
	public static IEqualityComparer<Option<T>> Default { get; } = new OptionEqualityComparer<T>(EqualityComparer<T>.Default);

	private readonly IEqualityComparer<T> _valueComparer;

	public OptionEqualityComparer(IEqualityComparer<T> valueComparer)
	{
		_valueComparer = valueComparer;
	}

	/// <inheritdoc />
	public int GetHashCode(Option<T> obj)
		=> obj.Type switch
		{
			OptionType.Undefined => int.MinValue,
			OptionType.None => int.MaxValue,
			OptionType.Some when obj.SomeOrDefault() is T value => _valueComparer.GetHashCode(value),
			_ => 0 // Some(default(T))
		};

	/// <inheritdoc />
	public bool Equals(Option<T> x, Option<T> y)
		=> y.Type switch
		{
			OptionType.None when x.IsNone() => true,
			OptionType.Undefined when x.IsUndefined() => true,
#pragma warning disable 8604 // Invalid?
			OptionType.Some when x.IsSome(out var xValue) => _valueComparer.Equals(xValue, y.SomeOrDefault()),
#pragma warning restore 8604
			_ => false,
		};
}
