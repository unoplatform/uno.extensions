using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Testing;

public readonly struct MessageConstraintPart<T>
{
	internal readonly ConstraintPart<IMessage>? Value;
	internal readonly MessageAxisConstraint? CurrentEntry;

	private MessageConstraintPart(ConstraintPart<IMessage>? value, MessageAxisConstraint? currentEntry)
	{
		Value = value;
		CurrentEntry = currentEntry;
	}

	public static implicit operator MessageConstraintPart<T>(Option<T> data)
		=> new Data<T>(data);

	public static implicit operator MessageConstraintPart<T>(T value)
		=> new Data<T>(value);

	public static implicit operator MessageConstraintPart<T>(Exception error)
		=> new Error(error);

	public static implicit operator MessageConstraintPart<T>(Type errorType)
		=> new Error(errorType);

	public static implicit operator MessageConstraintPart<T>(ConstraintPart<IMessage> part)
		=> new(part, default);

	public static implicit operator MessageConstraintPart<T>(MessageAxisConstraint currentEntryAxisConstraint)
		=> new(default, currentEntryAxisConstraint);

	public static implicit operator MessageConstraintPart<T>(MessageAxis axis)
		=> new Changed(axis);
}
