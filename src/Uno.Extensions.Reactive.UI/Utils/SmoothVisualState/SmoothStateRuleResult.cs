using System;
using System.Linq;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// The result of a <see cref="SmoothVisualStateRule"/>.
/// </summary>
public struct SmoothStateRuleResult
{
	/// <summary>
	/// The delay to wait before going to the next state.
	/// </summary>
	public TimeSpan? Delay { get; set; }

	/// <summary>
	/// The minimal duration to stay in the state.
	/// </summary>
	public TimeSpan? MinDuration { get; set; }
}
