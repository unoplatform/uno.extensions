#pragma warning disable CS1591 // XML Doc, will be moved elsewhere

using System;
using System.Linq;

namespace Uno.Extensions.Reactive.UI;

public struct SmoothStateRuleResult
{
	public TimeSpan? Delay { get; set; }

	public TimeSpan? MinDuration { get; set; }
}
