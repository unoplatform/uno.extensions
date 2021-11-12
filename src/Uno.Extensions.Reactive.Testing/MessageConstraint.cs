using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public class MessageConstraint<T>
{
	private readonly MessageEntryConstraint<T>? _previous;
	private readonly MessageEntryConstraint<T>? _current;
	private readonly MessageAxis[]? _changes;

	public MessageConstraint(MessageEntryConstraint<T>? previous, MessageEntryConstraint<T>? current, MessageAxis[]? changes)
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
				MessageEntryConstraint<T>.Initial.Assert(message.Previous);
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
				message.Changes.Should().BeEquivalentTo(_changes);
			}
		}
	}
}
