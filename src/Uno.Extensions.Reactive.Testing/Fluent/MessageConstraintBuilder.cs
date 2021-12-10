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

	public MessageConstraintBuilder<T> Changed(params MessageAxis[] axes)
	{
		_changes = axes;
		return this;
	}

	public MessageConstraintBuilder<T> Previous(params EntryAxisConstraint<T>[] axesConstraints)
	{
		_previous = new MessageEntryConstraint<T>(axesConstraints);
		return this;
	}

	public MessageConstraintBuilder<T> Current(params EntryAxisConstraint<T>[] axesConstraints)
	{
		_current = new MessageEntryConstraint<T>(axesConstraints);
		return this;
	}

	internal MessageConstraint<T> Build()
		=> new(_previous, _current, _changes);
}
