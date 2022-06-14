using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// A constraint on a <see cref="ChangeCollection"/>.
/// </summary>
/// <typeparam name="T">Type of the data of the feed</typeparam>
public readonly struct ChangedConstraint<T>
{
	public readonly ChangesConstraint Value;

	public ChangedConstraint(ChangesConstraint value)
	{
		Value = value;
	}

	public static implicit operator ChangedConstraint<T>(Changed axes)
		=> new(axes);

	public static implicit operator ChangedConstraint<T>(ChangesConstraint value)
		=> new(value);
}
