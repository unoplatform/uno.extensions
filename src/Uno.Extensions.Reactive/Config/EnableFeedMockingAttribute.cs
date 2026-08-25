using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Opt-in for the MVUX mocking metadata generation (spec 013). When present on an assembly, the MVUX
/// generator emits the mocking seams (dependency attributes, the view-model null-inject construction
/// path and the command seam) required by the external mocking generator. When absent, MVUX output is
/// byte-identical to the non-mocking output (additive, zero-cost opt-out).
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
