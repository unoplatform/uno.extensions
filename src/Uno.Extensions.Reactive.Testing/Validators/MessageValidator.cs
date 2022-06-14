using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.Extensions.Reactive.Testing;

public class MessageValidator<T>
{
	private readonly Constraint<MessageEntry<T>>? _previous;
	private readonly Constraint<MessageEntry<T>>? _current;
	private readonly Constraint<ChangeCollection>? _changes;

	public MessageValidator(Constraint<MessageEntry<T>>? previous, Constraint<MessageEntry<T>>? current, Constraint<ChangeCollection>? changes)
	{
		_previous = previous;
		_current = current;
		_changes = changes;
	}

	public void Assert(Message<T>? previous, Message<T> message)
	{
		using (AssertionScope.Current.ForContext("'Previous' entry"))
		{
			if (previous is null)
			{
				EntryValidator<T>.Initial.Assert(message.Previous);
			}
			else
			{
				(message.Previous as object).Should().Be(previous.Current);
			}

			if (_previous is not null)
			{
				_previous.Assert(message.Previous);
			}
		}

		if (_current is not null)
		{
			using (AssertionScope.Current.ForContext("'Current' entry"))
			{
				_current.Assert(message.Current);
			}
		}

		if (_changes is not null)
		{
			using (AssertionScope.Current.ForContext("reported changes"))
			{
				_changes.Assert(message.Changes);
			}
		}
	}
}
