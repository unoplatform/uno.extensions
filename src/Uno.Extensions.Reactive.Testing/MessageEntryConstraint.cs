using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public class MessageEntryConstraint<T>// : IEnumerable<MessageAxisConstraint<T>>
{
	private readonly IImmutableList<MessageAxisConstraint> _parts;

	public static MessageEntryConstraint<T> Initial { get; } = new(ImmutableList.Create<MessageAxisConstraint>(Data.Undefined, Error.No, Progress.Final));

	public MessageEntryConstraint(EntryAxisConstraint<T>[] axes)
		=> _parts = axes.Select(c => c.Value).ToImmutableList();

	public MessageEntryConstraint(IImmutableList<MessageAxisConstraint> parts)
		=> _parts = parts;

	public void Assert(MessageEntry<T> entry)
	{
		foreach (var constraint in _parts)
		{
			using (AssertionScope.Current.ForContext("'" + constraint.Axis.Identifier + "'"))
			{
				constraint.Assert(entry);
			}
		}
	}

	public static implicit operator MessageEntryConstraint<T>(Option<T> data)
		=> new(ImmutableList.Create<MessageAxisConstraint>(new Data<T>(data)));

	public static implicit operator MessageEntryConstraint<T>(T value)
		=> new(ImmutableList.Create<MessageAxisConstraint>(new Data<T>(value)));

	public static implicit operator MessageEntryConstraint<T>(Exception error)
		=> new(ImmutableList.Create<MessageAxisConstraint>(new Error(error)));

	public static implicit operator MessageEntryConstraint<T>(Type errorType)
		=> new(ImmutableList.Create<MessageAxisConstraint>(new Error(errorType)));

	public static implicit operator MessageEntryConstraint<T>(MessageAxisConstraint axisConstraint)
		=> new(ImmutableList.Create<MessageAxisConstraint>(axisConstraint));

	//public static implicit operator MessageEntryConstraint<T>(MessageAxisConstraint<T> axisConstraint)
	//	=> new(ImmutableList.Create<ConstraintPart<IMessageEntry>>(axisConstraint));

	//public static implicit operator MessageEntryConstraint<T>(MessageAxisConstraint<T>[] axisConstraints)
	//	=> new(ImmutableList.CreateRange(axisConstraints));

	//public static implicit operator MessageEntryConstraint<T>(List<MessageAxisConstraint<T>> axisConstraints)
	//	=> new(ImmutableList.CreateRange(axisConstraints));

	public static MessageEntryConstraint<T> operator &(MessageEntryConstraint<T> left, MessageEntryConstraint<T> right)
		=> new(left._parts.AddRange(right._parts));

	public static MessageEntryConstraint<T> operator &(MessageEntryConstraint<T> left, Option<T> data)
		=> new(left._parts.Add(new Data<T>(data)));

	public static MessageEntryConstraint<T> operator &(MessageEntryConstraint<T> left, T value)
		=> new(left._parts.Add(new Data<T>(value)));

	public static MessageEntryConstraint<T> operator &(MessageEntryConstraint<T> left, Exception error)
		=> new(left._parts.Add(new Error(error)));

	public static MessageEntryConstraint<T> operator &(MessageEntryConstraint<T> left, Type errorType)
		=> new(left._parts.Add(new Error(errorType)));

	public static MessageEntryConstraint<T> operator &(MessageEntryConstraint<T> left, MessageAxisConstraint axisConstraint)
		=> new(left._parts.Add(axisConstraint));

	///// <inheritdoc />
	//public IEnumerator<ConstraintPart<IMessageEntry>> GetEnumerator()
	//	=> _axesConstraints.GetEnumerator();

	///// <inheritdoc />
	//IEnumerator IEnumerable.GetEnumerator()
	//	=> GetEnumerator();
}
