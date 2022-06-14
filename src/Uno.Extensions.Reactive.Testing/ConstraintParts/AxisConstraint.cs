using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// A constraint on a <see cref="MessageEntry{T}"/>
/// </summary>
/// <typeparam name="T">Type of the data of the feed</typeparam>
public readonly struct AxisConstraint<T>
{
	public readonly AxisConstraint Value;

	private AxisConstraint(AxisConstraint constraint)
		=> Value = constraint;

	public static implicit operator AxisConstraint<T>(Option<T> data)
		=> new Data<T>(data);

	public static implicit operator AxisConstraint<T>(T value)
		=> new Data<T>(value);

	public static implicit operator AxisConstraint<T>(T[] value)
		=> new ItemsConstraint<T>(value);

	public static implicit operator AxisConstraint<T>(Exception error)
		=> new Error(error);

	public static implicit operator AxisConstraint<T>(Type errorType)
		=> new Error(errorType);

	public static implicit operator AxisConstraint<T>(AxisConstraint part)
		=> new(part);
}
