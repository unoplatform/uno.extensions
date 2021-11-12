using System;
using System.Linq;
using FluentAssertions;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public sealed class Progress : MessageAxisConstraint
{
	public static Progress Transient { get; } = new(true);

	public static Progress Final { get; } = new(false);

	private readonly bool _isTransient;

	private Progress(bool isTransient)
	{
		_isTransient = isTransient;
	}

	/// <inheritdoc />
	public override MessageAxis Axis => MessageAxis.Progress;

	/// <inheritdoc />
	public override void Assert(IMessageEntry entry)
	{
		entry.IsTransient.Should().Be(_isTransient);
	}
}
