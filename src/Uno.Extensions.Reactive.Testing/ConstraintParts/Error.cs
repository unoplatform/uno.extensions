using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public sealed class Error : AxisConstraint
{
	public static Error No { get; } = new();

	private readonly Exception? _error;
	private readonly Type? _errorType;

	private Error()
	{
	}

	public Error(Exception error)
	{
		_error = error;
	}

	public Error(Type errorType)
	{
		_errorType = errorType;
	}

	/// <inheritdoc />
	public override MessageAxis ConstrainedAxis => MessageAxis.Error;

	/// <inheritdoc />
	public override void Assert(IMessageEntry entry)
	{
		if (_error is not null)
		{
			entry.Error.Should().Be(_error);
		}
		else if (_errorType is not null)
		{
			entry.Error.Should().NotBeNull($"an exception of type {_errorType.Name} was expected.");
			if (entry.Error is not null && !_errorType.IsInstanceOfType(entry.Error))
			{
				AssertionScope.Current.Fail($"an exception of type {_errorType.Name} was expected, but get an exception of type {entry.Error.GetType().Name}.");
			}
		}
		else
		{
			entry.Error.Should().BeNull();
		}
	}

	public static implicit operator Error(Exception error)
		=> new(error);

	public static implicit operator Error(Type errorType)
		=> new(errorType);
}
