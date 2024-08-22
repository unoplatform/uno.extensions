using System;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Configuration for the generation tool of bindable view models
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class BindableGenerationToolAttribute : Attribute
{
	// Latest version is 3, keeping version 2 as default for backward compatibility
	private const int DefaultVersion = 2;

	/// <summary>
	/// Creates a new BindableGenerationTool object.
	/// </summary>
	/// <param name="version">The version of the tool to use when generating the bindables</param>
	public BindableGenerationToolAttribute(int version = DefaultVersion)
	{
		Version = version;
	}

	/// <summary>
	/// Gets or sets the version of tool that should be used to generate bindables.
	/// </summary>
	/// <remarks>Set this to 1 to use code gen used in Uno.Extensions.Reactive versions below 2.3</remarks>
	public int Version { get; init; }
}
