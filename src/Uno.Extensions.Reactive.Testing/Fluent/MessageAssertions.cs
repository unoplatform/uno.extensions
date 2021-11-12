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

	public void Be(params EntryAxisConstraint<T>[] assertions)
		=> _message.Current.Should().Be(assertions);
}
