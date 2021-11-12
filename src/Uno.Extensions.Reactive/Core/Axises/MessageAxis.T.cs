using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Uno.Extensions.Reactive;

public sealed class MessageAxis<T> : MessageAxis
{
	internal delegate T Aggregator(IReadOnlyCollection<T> values);

	private readonly Aggregator _aggregate;

	internal MessageAxis(string name, Aggregator aggregate)
		: base(name)
	{
		_aggregate = aggregate;
	}

	[Pure]
	public T? FromMessageValue(MessageAxisValue value)
		=> value is { IsSet: true } and { Value: T raw } ? raw : default;

	[Pure]
	public MessageAxisValue ToMessageValue(T? value)
		=> value is null ? default : new(value);

	/// <inheritdoc />
	protected internal override MessageAxisValue Aggregate(IEnumerable<MessageAxisValue> values)
	{
		var items = values
			.Where(value => value is { IsSet: true } and { Value: T })
			.Select(value => (T)value.Value!)
			.ToList();

		return items.Count switch
		{
			0 => MessageAxisValue.Unset,
			1 => new MessageAxisValue(items[0]!),
			_ => new MessageAxisValue(_aggregate(items)!)
		};
	}

	/// <inheritdoc />
	protected internal override bool AreEquals(MessageAxisValue left, MessageAxisValue right)
	{
		if (left.IsSet != right.IsSet)
		{
			return false;
		}
		else if (!left.IsSet)
		{
			return true;
		}
		else
		{
			return left.Value! == right.Value!;
		}
	}
}
