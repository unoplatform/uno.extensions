using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions.Reactive.Testing;

namespace FluentAssertions;

public class MessageRecorderConstraintBuilder<T>
{
	private readonly List<MessageValidator<T>> _messagesConstraints = new();

	public MessageRecorderConstraintBuilder<T> Message(Action<MessageConstraintBuilder<T>> constraintBuilder)
	{
		var builder = new MessageConstraintBuilder<T>();
		constraintBuilder(builder);

		_messagesConstraints.Add(builder.Build());

		return this;
	}

	public MessageRecorderConstraintBuilder<T> Message(params MessageConstraint<T>[] constraints)
	{
		var currentParts = constraints
			.Where(c => c.CurrentEntry is not null)
			.Select(c => c.CurrentEntry!)
			.ToImmutableList();

		var changes = constraints
			.Where(c => c.Changes is not null)
			.Select(c => c.Changes!)
			.ToImmutableList();

		_messagesConstraints.Add(new MessageValidator<T>(default, new EntryValidator<T>(currentParts), new ChangesValidator<T>(changes)));

		return this;
	}

	internal RecorderValidator<T> Build()
		=> new(_messagesConstraints.ToArray());
}
