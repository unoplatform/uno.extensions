using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

internal class MessageAxisUpdate
{
	public bool IsOverride { get; set; }

	public MessageAxis Axis { get; }

	public MessageAxisValue Value { get; set; }

	public MessageAxisUpdate(MessageAxis axis, MessageAxisValue value)
	{
		Axis = axis;
		Value = value;
	}

	public MessageAxisValue GetValue(MessageAxisValue parent, MessageAxisValue current)
		=> IsOverride ? Value : Axis.GetLocalValue(parent, Value);
}
