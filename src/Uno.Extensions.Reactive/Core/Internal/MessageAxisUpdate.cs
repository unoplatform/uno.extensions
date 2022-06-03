using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

internal class MessageAxisUpdate
{
	public bool IsOverride { get; set; }

	public MessageAxis Axis { get; }

	public MessageAxisValue Value { get; set; }

	public IChangeSet? Changes { get; }

	public MessageAxisUpdate(MessageAxis axis, MessageAxisValue value, IChangeSet? changes = null)
	{
		Axis = axis;
		Value = value;
		Changes = changes;
	}

	public MessageAxisValue GetValue(MessageAxisValue parent, MessageAxisValue current)
		=> IsOverride ? Value : Axis.GetLocalValue(parent, Value);
}
