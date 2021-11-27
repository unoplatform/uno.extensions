using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive;

public class FeedViewVisualStateSelector
{
	protected internal virtual IEnumerable<(string stateName, bool shouldUseTransition)> GetVisualStates(IMessage message)
		=> message
			.Changes
			.Select(axis => (name: GetVisualState(axis, message.Current[axis])!, true))
			.Where(state => !string.IsNullOrWhiteSpace(state.name));

	protected virtual string? GetVisualState(MessageAxis axis, MessageAxisValue value)
		=> axis.Identifier switch
		{
			MessageAxes.Data => MessageAxis.Data.FromMessageValue(value).Type.ToString(),

			MessageAxes.Error when value.IsSet => "Error",
			MessageAxes.Error => "NoError",

			MessageAxes.Progress when MessageAxis.Progress.FromMessageValue(value) => "Indeterminate",
			MessageAxes.Progress => "NoProgress",

			_ => value.IsSet ? axis.Identifier + "_" + value.Value! : axis.Identifier
		};
}
