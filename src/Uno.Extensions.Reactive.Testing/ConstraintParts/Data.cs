using System;
using System.Linq;
using FluentAssertions;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public sealed class Data : MessageAxisConstraint
{
	public static Data Undefined { get; } = new(OptionType.Undefined);

	public static Data None { get; } = new(OptionType.None);

	public static Data Some { get; } = new(OptionType.Some);

	private readonly OptionType _expectedType;

	private Data(OptionType expectedType)
	{
		_expectedType = expectedType;
	}

	/// <inheritdoc />
	public override MessageAxis Axis => MessageAxis.Data;

	/// <inheritdoc />
	public override void Assert(IMessageEntry entry)
	{
		entry.Data.Type.Should().Be(_expectedType);
	}
}
