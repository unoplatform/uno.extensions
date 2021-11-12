using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions.Reactive.Testing;

namespace FluentAssertions;

public class MessageRecorderConstraintBuilder<T>
{
	private readonly List<MessageConstraint<T>> _messagesConstraints = new();

	public MessageRecorderConstraintBuilder<T> Message(Action<MessageConstraintBuilder<T>> constraintBuilder)
	{
		var builder = new MessageConstraintBuilder<T>();
		constraintBuilder(builder);

		_messagesConstraints.Add(builder.Build());

		return this;
	}

	public MessageRecorderConstraintBuilder<T> Message(params MessageConstraintPart<T>[] parts)
	{
		var currentParts = parts
			.Where(p => p.CurrentEntry is not null)
			.Select(p => p.CurrentEntry!)
			.ToImmutableList();
		var changes = parts
			.Select(p => p.Value as Changed)
			.Where(change => change is not null)
			.ToList() is {Count: >0} configChanges
			? configChanges.Aggregate(Changed.None , (total, change) => total & change!)
			: default;

		_messagesConstraints.Add(new MessageConstraint<T>(default, new MessageEntryConstraint<T>(currentParts), changes?.Expected));

		return this;
	}

	internal MessageRecorderConstraint<T> Build()
		=> new(_messagesConstraints.ToArray());
}
