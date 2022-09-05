using System;
using System.Linq;
using FluentAssertions;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public sealed class Data<T> : AxisConstraint
{
	private readonly Option<T> _data;

	public static Data<T> Some(Option<T> data)
		=> new(data);

	public Data(Option<T> data)
	{
		_data = data;
	}

	/// <inheritdoc />
	public override MessageAxis ConstrainedAxis => MessageAxis.Data;

	/// <inheritdoc />
	public override void Assert(IMessageEntry entry)
	{
		((Option<T>)entry.Data).Should().Be(_data);
	}

	public static implicit operator Data<T>(Option<T> data)
		=> new(data);

	public static implicit operator Data<T>(T value)
		=> new(value);
}
