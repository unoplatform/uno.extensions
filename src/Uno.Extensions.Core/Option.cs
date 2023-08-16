#pragma warning disable CS1591 // XML Doc, will be moved elsewhere

using System;
using System.Linq;

namespace Uno.Extensions;

public static class Option
{
	public static Option<T> SomeOrNone<T>(T? value)
		=> value is null ? Option<T>.None() : Option<T>.Some(value);

	public static Option<T> SomeOrNone<T>(T? value)
		where T : struct
		=> value is null ? Option<T>.None() : Option<T>.Some(value.Value);
	public static Option<T> Some<T>(T value)
		=> Option<T>.Some(value);

	public static Option<T> None<T>()
		=> Option<T>.None();

	public static Option<T> Undefined<T>()
		=> Option<T>.Undefined();
}
