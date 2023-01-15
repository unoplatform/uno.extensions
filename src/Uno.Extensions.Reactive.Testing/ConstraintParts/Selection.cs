using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.Extensions.Reactive.Testing;

public class Selection : AxisConstraint
{
	public static Selection Undefined { get; } = new(isDefined: false, isEmpty: null);

	public static Selection Empty { get; } = new(isDefined: true, isEmpty: true);

	public static Selection<T> Items<T>(params T[] selectedItems)
		=> new(selectedItems);

	private readonly bool _isDefined;
	private readonly bool? _isEmpty;

	private protected Selection(bool isDefined, bool? isEmpty)
	{
		_isDefined = isDefined;
		_isEmpty = isEmpty;
	}

	/// <inheritdoc />
	public override MessageAxis ConstrainedAxis => MessageAxis.Selection;

	/// <inheritdoc />
	public override void Assert(IMessageEntry actual)
	{
		var actualSelection = actual.Get(MessageAxis.Selection);

		using (AssertionScope.Current.ForContext("is defined"))
		{
			(actualSelection is not null).Should().Be(_isDefined);
		}

		if (actualSelection is not null && _isEmpty is not null)
		{
			using (AssertionScope.Current.ForContext("is empty"))
			{
				actualSelection.IsEmpty.Should().Be(_isEmpty.Value);
			}
		}
	}
}

public class Selection<T> : Selection
{
	private readonly T[] _selectedItems;

	public Selection(T[] selectedItems)
		: base(isDefined: true, isEmpty: false)
	{
		_selectedItems = selectedItems;
	}

	/// <inheritdoc />
	public override MessageAxis ConstrainedAxis => MessageAxis.Selection;

	/// <inheritdoc />
	public override void Assert(IMessageEntry actual)
	{
		base.Assert(actual);

		var actualItems = (IImmutableList<T>)actual.Data.SomeOrDefault(ImmutableList<T>.Empty);
		var actualSelection = actual.Get(MessageAxis.Selection) ?? SelectionInfo.Empty;

		using (AssertionScope.Current.ForContext("items"))
		{
			var selectedItems = actualSelection.GetSelectedItems(actualItems, failIfOutOfRange: true);

			selectedItems.Should().BeEquivalentTo(_selectedItems);
		}
	}
}
