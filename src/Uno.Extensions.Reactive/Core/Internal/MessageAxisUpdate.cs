using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

internal class MessageAxisUpdate
{
	public MessageAxis Axis { get; }

	public MessageAxisValue Value { get; set; }

	public IChangeSet? Changes { get; }

	public MessageAxisUpdate(MessageAxis axis, MessageAxisValue value, IChangeSet? changes = null)
	{
		Axis = axis;
		Value = value;
		Changes = changes;
	}

	public (MessageAxisValue value, IChangeSet? changes) GetValue(MessageAxisValue parent, MessageAxisValue current)
		=> Axis.GetLocalValue(parent, current, (Value, Changes));

	/// <inheritdoc />
	public override string ToString()
		=> $"{Axis} = {Value} {(Changes is not null ? "(with details)" : "")}";
}
