using System;
using System.Linq;
using FluentAssertions.Primitives;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive;

namespace FluentAssertions;

public class MessageAssertions<T> : ReferenceTypeAssertions<Message<T>, MessageAssertions<T>>
{
	private readonly Message<T> _message;

	public MessageAssertions(Message<T> message)
		: base(message)
	{
		_message = message;
	}

	/// <inheritdoc />
	protected override string Identifier { get; } = typeof(Message<T>).Name;

	public void Be(params AxisConstraint<T>[] assertions)
		=> _message.Current.Should().Be(assertions);

	public void Be(Action<MessageConstraintBuilder<T>> constraintBuilder)
	{
		var builder = new MessageConstraintBuilder<T>();
		constraintBuilder(builder);

		builder.Build().Assert(null, _message);
	}
}
