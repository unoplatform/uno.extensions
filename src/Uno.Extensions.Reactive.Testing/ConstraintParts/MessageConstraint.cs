using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// A constraint than can be either on <see cref="Message{T}.Current"/> either on <see cref="Message{T}.Changes"/>.
/// </summary>
/// <typeparam name="T">Type of the data of the feed.</typeparam>
/// <remarks>
/// This is only an helper class used by the <see cref="FluentAssertions.MessageRecorderConstraintBuilder{T}"/>
/// in order to allow implicit casting of well known constraints.
/// </remarks>
public readonly struct MessageConstraint<T>
{
	internal readonly ChangesConstraint? Changes;
	internal readonly AxisConstraint? CurrentEntry;

	private MessageConstraint(ChangesConstraint? changes, AxisConstraint? currentEntry)
	{
		Changes = changes;
		CurrentEntry = currentEntry;
	}

	// Current entry assertion
	public static implicit operator MessageConstraint<T>(Option<T> data)
		=> new Data<T>(data);

	public static implicit operator MessageConstraint<T>(T value)
		=> new Data<T>(value);

	public static implicit operator MessageConstraint<T>(T[] value)
		=> new ItemsConstraint<T>(value);

	public static implicit operator MessageConstraint<T>(Exception error)
		=> new Error(error);

	public static implicit operator MessageConstraint<T>(Type errorType)
		=> new Error(errorType);

	public static implicit operator MessageConstraint<T>(AxisConstraint currentEntryConstraint)
		=> new(default, currentEntryConstraint);

	// Changes assertion
	public static implicit operator MessageConstraint<T>(MessageAxis axis)
		=> new Changed(axis);

	public static implicit operator MessageConstraint<T>(ChangesConstraint changeConstraint)
		=> new(changeConstraint, default);
}
