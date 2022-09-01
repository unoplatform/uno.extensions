using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// Selector to customize the visual states applied for each message received by <see cref="FeedView"/>.
/// </summary>
public class FeedViewVisualStateSelector
{
	/// <summary>
	/// Gets all visual states to apply for a given message.
	/// </summary>
	/// <param name="feedView">The feed view for which the states should be selected.</param>
	/// <param name="message">The message to render.</param>
	/// <returns>Visual states to apply.</returns>
	protected internal virtual IEnumerable<(string stateName, bool shouldUseTransition)> GetVisualStates(FeedView feedView, IMessage message)
		=> message
			.Changes
			.Select(axis => (name: GetVisualState(feedView, message, axis, message.Current[axis])!, true))
			.Where(state => !string.IsNullOrWhiteSpace(state.name));

	/// <summary>
	/// Gets the visual state to apply for a given axis regarding its current value.
	/// </summary>
	/// <param name="feedView">The feed view for which the state should be selected.</param>
	/// <param name="message">The message to render.</param>
	/// <param name="axis">The axis.</param>
	/// <param name="value">The metadata raw value.</param>
	/// <returns>The state to apply, or null if none.</returns>
	protected virtual string? GetVisualState(FeedView feedView, IMessage message, MessageAxis axis, MessageAxisValue value)
		=> axis.Identifier switch
		{
			MessageAxes.Data => MessageAxis.Data.FromMessageValue(value).Type.ToString(),

			MessageAxes.Error when value.IsSet => "Error",
			MessageAxes.Error => "NoError",

			MessageAxes.Progress when MessageAxis.Progress.FromMessageValue(value) && (!message.Current[MessageAxis.Refresh].IsSet || feedView.RefreshingState is FeedViewRefreshState.Loading) => "Indeterminate",
			MessageAxes.Progress => "NoProgress",

			MessageAxes.BindingSource => null,
			MessageAxes.Refresh => null,

			_ => value.IsSet ? axis.Identifier + "_" + value.Value! : axis.Identifier
		};
}
