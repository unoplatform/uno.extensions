using System;
using System.Linq;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public abstract class AxisConstraint : Constraint<IMessageEntry>
{
	public abstract MessageAxis Axis { get; }
}
