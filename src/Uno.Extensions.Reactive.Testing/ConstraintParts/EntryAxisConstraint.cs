using System;
using System.Data;
using System.Linq;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public readonly struct EntryAxisConstraint<T>
{
	public readonly MessageAxisConstraint Value;

	private EntryAxisConstraint(MessageAxisConstraint constraint)
		=> Value = constraint;

	public static implicit operator EntryAxisConstraint<T>(Option<T> data)
		=> new Data<T>(data);

	public static implicit operator EntryAxisConstraint<T>(T value)
		=> new Data<T>(value);

	public static implicit operator EntryAxisConstraint<T>(Exception error)
		=> new Error(error);

	public static implicit operator EntryAxisConstraint<T>(Type errorType)
		=> new Error(errorType);

	public static implicit operator EntryAxisConstraint<T>(MessageAxisConstraint part)
		=> new(part);
}
