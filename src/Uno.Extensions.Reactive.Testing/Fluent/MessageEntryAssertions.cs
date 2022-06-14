using System;
using System.Linq;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive;

namespace FluentAssertions;

public class MessageEntryAssertions<T> : ReferenceTypeAssertions<MessageEntry<T>, MessageEntryAssertions<T>>
{
	public MessageEntryAssertions(MessageEntry<T> entry)
		: base(entry)
	{
	}

	/// <inheritdoc />
	protected override string Identifier { get; } = typeof(MessageEntry<T>).Name;

	public void Be(params AxisConstraint<T>[] constraints)
	{
		using (new AssertionScope(Identifier))
		{
			new EntryValidator<T>(constraints).Assert(Subject);
		}
	}
}
