using System;
using System.Linq;
using FluentAssertions;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public sealed class Error : MessageAxisConstraint
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
	public override MessageAxis Axis => MessageAxis.Error;

	/// <inheritdoc />
	public override void Assert(IMessageEntry entry)
	{
		if (_error is not null)
		{
			entry.Error.Should().Be(_error);
		}
		else if (_errorType is not null)
		{
			entry.Error.Should().NotBeNull($"an exception of type {_errorType} was expected.");
			_errorType.IsInstanceOfType(entry.Error).Should().BeTrue($"an exception of type {_errorType} was expected.");
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
