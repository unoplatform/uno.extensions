using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Defines a metadata axis of a <see cref="MessageEntry{T}"/>
/// </summary>
public sealed class MessageAxis<T> : MessageAxis
{
	internal delegate T Aggregator(IReadOnlyCollection<T> values);

	private readonly Aggregator _aggregate;

	internal MessageAxis(string name, Aggregator aggregate)
		: base(name)
	{
		_aggregate = aggregate;
	}

	/// <summary>
	/// Get the metadata from the raw axis value.
	/// </summary>
	/// <param name="value">The raw axis value.</param>
	/// <returns>The metadata.</returns>
	[Pure]
	public T? FromMessageValue(MessageAxisValue value)
		=> value is { IsSet: true } and { Value: T raw } ? raw : default;

	/// <summary>
	/// Encapsulates a metadata into a raw axis value..
	/// </summary>
	/// <param name="value">The metadata to encapsulate..</param>
	/// <returns>The raw axis value.</returns>
	[Pure]
	public MessageAxisValue ToMessageValue(T value)
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
