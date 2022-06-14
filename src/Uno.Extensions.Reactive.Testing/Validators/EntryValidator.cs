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

/// <summary>
/// Validates a set of constraints on a <see cref="MessageEntry{T}"/>.
/// </summary>
/// <typeparam name="T">Type of the data of the entry.</typeparam>
public class EntryValidator<T> : Constraint<MessageEntry<T>>
{
	private readonly IImmutableList<AxisConstraint> _constraints;

	public static EntryValidator<T> Initial { get; } = new(ImmutableList.Create<AxisConstraint>(Data.Undefined, Error.No, Progress.Final));

	public EntryValidator(AxisConstraint<T>[] axes)
		=> _constraints = axes.Select(c => c.Value).ToImmutableList();

	public EntryValidator(IImmutableList<AxisConstraint> constraints)
		=> _constraints = constraints;

	public override void Assert(MessageEntry<T> entry)
	{
		foreach (var constraint in _constraints)
		{
			using (AssertionScope.Current.ForContext("'" + constraint.Axis.Identifier + "'"))
			{
				constraint.Assert(entry);
			}
		}
	}

	public static implicit operator EntryValidator<T>(Option<T> data)
		=> new(ImmutableList.Create<AxisConstraint>(new Data<T>(data)));

	public static implicit operator EntryValidator<T>(T value)
		=> new(ImmutableList.Create<AxisConstraint>(new Data<T>(value)));

	public static implicit operator EntryValidator<T>(T[] value)
		=> new(ImmutableList.Create<AxisConstraint>(new ItemsConstraint<T>(value)));

	public static implicit operator EntryValidator<T>(Exception error)
		=> new(ImmutableList.Create<AxisConstraint>(new Error(error)));

	public static implicit operator EntryValidator<T>(Type errorType)
		=> new(ImmutableList.Create<AxisConstraint>(new Error(errorType)));

	public static implicit operator EntryValidator<T>(AxisConstraint axisConstraint)
		=> new(ImmutableList.Create<AxisConstraint>(axisConstraint));
}
