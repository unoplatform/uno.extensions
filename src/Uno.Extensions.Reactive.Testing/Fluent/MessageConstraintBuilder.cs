using System;
using System.Linq;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive;

namespace FluentAssertions;

public class MessageConstraintBuilder<T>
{
	private MessageEntryConstraint<T>? _previous;
	private MessageEntryConstraint<T>? _current;
	private MessageAxis[]? _changes;

	public MessageConstraintBuilder<T> Changed(params MessageAxis[] axises)
	{
		_changes = axises;
		return this;
	}

	public MessageConstraintBuilder<T> Previous(params EntryAxisConstraint<T>[] axisesConstraints)
	{
		_previous = new MessageEntryConstraint<T>(axisesConstraints);
		return this;
	}

	public MessageConstraintBuilder<T> Current(params EntryAxisConstraint<T>[] axisesConstraints)
	{
		_current = new MessageEntryConstraint<T>(axisesConstraints);
		return this;
	}

	internal MessageConstraint<T> Build()
		=> new(_previous, _current, _changes);
}
