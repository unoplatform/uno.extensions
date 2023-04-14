using System;
using System.Linq;

namespace Uno.Extensions.Reactive.UI;

/// <summary>
/// Flags a <see cref="Control"/> that is aware of the <see cref="SmoothVisualStateManager"/> and can provide additional information to it.
/// </summary>
public interface ISmoothVisualStateAware
{
	/// <summary>
	/// Flag that indicates that considering the state of the control, state should be applied without any delay.
	/// </summary>
	public bool ShouldGoToStateSync { get; }
}
