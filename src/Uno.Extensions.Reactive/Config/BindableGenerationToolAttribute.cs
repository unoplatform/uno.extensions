using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Configuration for the generation tool of bindable view models
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class BindableGenerationToolAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the version of tool that should be used to generate bindables.
	/// </summary>
	/// <remarks>Set this to 1 to use code gen used in Uno.Extensions.Reactive versions below 2.3</remarks>
	public int Version { get; init; } = 3;
}
