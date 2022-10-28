using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Indicates if types that matches defined suffixes should be automatically exposed as bindable friendly view models.
/// </summary>
/// <remarks>If disabled, you can still generates bindable friendly view model by flagging a class with the <see cref="ReactiveBindableAttribute"/>.</remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public class ImplicitBindablesAttribute : Attribute
{
	/// <summary>
	/// Gets the legacy pattern that was used in versions prior to 2.3.
	/// </summary>
	public const string LegacyPattern = "ViewModel$";

	/// <summary>
	/// Gets or sets a bool which indicates if the generation of view models based on class names is enabled of not.
	/// </summary>
	public bool IsEnabled { get; init; } = true;

	/// <summary>
	/// The patterns that the class FullName has to match to implicitly trigger view model generation.
	/// </summary>
	public string[] Patterns { get; } = { "Model$" };

	/// <summary>
	/// Create a new instance using default values.
	/// </summary>
	public ImplicitBindablesAttribute()
	{
	}

	/// <summary>
	/// Creates a new instance specifying the <see cref="Patterns"/>.
	/// </summary>
	/// <param name="patterns">The patterns that the class FullName has to match to implicitly trigger view model generation.</param>
	public ImplicitBindablesAttribute(params string[] patterns)
	{
		Patterns = patterns;
	}
}
