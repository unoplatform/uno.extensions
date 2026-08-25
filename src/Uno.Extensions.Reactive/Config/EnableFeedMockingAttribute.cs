using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Configures MVUX mocking metadata generation (spec 013). The instrumentation (dependency attributes
/// + the command seam) is emitted <b>by default</b> — the runtime, not the generator, decides whether
/// mocking is active (via <c>MockingService.Enable()</c>). Add <c>[assembly: EnableFeedMocking(IsEnabled = false)]</c>
/// to opt out and restore byte-identical MVUX output. Follows the same on-by-default / opt-out model as
/// the other MVUX generation attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class EnableFeedMockingAttribute : Attribute
{
	/// <summary>
	/// Gets or sets a value indicating whether the mocking metadata generation is enabled.
	/// </summary>
	public bool IsEnabled { get; init; } = true;

	/// <summary>
	/// Creates a new instance enabling mocking metadata generation.
	/// </summary>
	public EnableFeedMockingAttribute()
	{
	}
}
