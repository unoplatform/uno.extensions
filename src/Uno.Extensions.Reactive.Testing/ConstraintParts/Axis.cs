using System;
using System.Linq;
using FluentAssertions;

namespace Uno.Extensions.Reactive.Testing;

public sealed class Axis : AxisConstraint
{
	public static Axis Set<T>(MessageAxis<T> axis) => new(axis, true, false, default);

	public static Axis Set<T>(MessageAxis<T> axis, T expectedValue) => new(axis, true, true, expectedValue);

	public static Axis NotSet<T>(MessageAxis<T> axis) => new(axis, false, false, default);

	private readonly bool _isSet;
	private readonly bool _hasExpectedValue;
	private readonly object? _expectedValue;

	public Axis(MessageAxis axis, bool isSet, bool hasExpectedValue, object? expectedValue)
	{
		ConstrainedAxis = axis;
		_isSet = isSet;
		_hasExpectedValue = hasExpectedValue;
		_expectedValue = expectedValue;
	}

	/// <inheritdoc />
	public override MessageAxis ConstrainedAxis { get; }

	/// <inheritdoc />
	public override void Assert(IMessageEntry actual)
	{
		var actualAxisValue = actual[ConstrainedAxis];

		actualAxisValue.IsSet.Should().Be(_isSet);
		if (_hasExpectedValue)
		{
			actualAxisValue.Value.Should().Be(_expectedValue);
		}
	}
}
