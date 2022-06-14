using System;
using System.Linq;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive;

namespace FluentAssertions;

public class MessageConstraintBuilder<T>
{
	private EntryValidator<T>? _previous;
	private EntryValidator<T>? _current;
	private Constraint<ChangeCollection>? _changes;

	public MessageConstraintBuilder<T> Changed(params ChangedConstraint<T>[] axes)
	{
		_changes = new ChangesValidator<T>(axes);
		return this;
	}

	public MessageConstraintBuilder<T> Previous(params AxisConstraint<T>[] axesConstraints)
	{
		_previous = new EntryValidator<T>(axesConstraints);
		return this;
	}

	public MessageConstraintBuilder<T> Current(params AxisConstraint<T>[] axesConstraints)
	{
		_current = new EntryValidator<T>(axesConstraints);
		return this;
	}

	internal MessageValidator<T> Build()
		=> new(_previous, _current, _changes);
}
