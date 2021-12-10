using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;

namespace Uno.Extensions.Reactive.Testing;

public class Changed : ConstraintPart<IMessage>
{
	public static Changed None { get; } = new();

	public static Changed Data { get; } = new(MessageAxis.Data);

	public static Changed Error { get; } = new(MessageAxis.Error);

	public static Changed Progress { get; } = new(MessageAxis.Progress);

	public static Changed Axes(params MessageAxis[] axes) 
		=> new(axes);

	public readonly MessageAxis[] Expected;

	public Changed(params MessageAxis[] expected)
		=> Expected = expected;

	/// <inheritdoc />
	public override void Assert(IMessage message)
		=> message.Changes.Should().BeEquivalentTo(Expected);

	public static Changed operator &(Changed left, Changed right)
		=> new(left.Expected.Concat(right.Expected).ToArray());
}
