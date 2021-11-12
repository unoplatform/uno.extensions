using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public struct SmoothStateRuleResult
{
	public TimeSpan? Delay { get; set; }

	public TimeSpan? MinDuration { get; set; }
}
