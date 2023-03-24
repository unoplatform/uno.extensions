using System;
using System.Linq;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// A rule used by a <see cref="SmoothVisualStateManager"/> to determine the delay to wait before going to the target state.
/// </summary>
public class SmoothVisualStateRule
{
	/// <summary>
	/// The name of the <see cref="VisualStateGroup"/> to which this rule applies to.
	/// `null` to apply to any group.
	/// </summary>
	public string? Group { get; set; }

	/// <summary>
	/// The name of the current <see cref="VisualState"/> for this rule to be considered.
	/// `null` to apply to any visual state.
	/// </summary>
	public string? From { get; set; }

	/// <summary>
	/// The name of the target <see cref="VisualState"/> for this rule to be considered.
	/// `null` to apply to any visual state.
	/// </summary>
	public string? To { get; set; }

	/// <summary>
	/// The delay to wait before going to the target state.
	/// `null` to not apply any delay.
	/// </summary>
	public TimeSpan? Delay { get; set; }

	/// <summary>
	/// The minimum duration to stay in the target state before moving to any next state.
	/// `null` to not apply any minimum duration.
	/// </summary>
	public TimeSpan? MinDuration { get; set; }

	/// <summary>
	/// Gets the <see cref="SmoothStateRuleResult"/> for the given <paramref name="group"/>, <paramref name="current"/> and <paramref name="target"/> states.
	/// </summary>
	/// <param name="group">The visual state group for which this rule should be applied.</param>
	/// <param name="current">The current visual state, if any.</param>
	/// <param name="target">The target visual state</param>
	/// <returns>The delay and minimum duration that should be applied to go to the target visual state.</returns>
	public SmoothStateRuleResult Get(VisualStateGroup group, VisualState? current, VisualState target)
	{
		if (Group is not null && group.Name != Group)
		{
			return default;
		}

		if (From is not null && current?.Name != From)
		{
			return default;
		}

		if (To is not null && target.Name != To)
		{
			return default;
		}

		return new() { Delay = Delay, MinDuration = MinDuration };
	}
}
