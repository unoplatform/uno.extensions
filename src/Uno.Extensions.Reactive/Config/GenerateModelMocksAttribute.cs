using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Enables generation of mock factories (<c>CreateMock</c> and the <c>{Vm}Mocks</c> bundle) for the bindable view models of this assembly.
/// </summary>
/// <remarks>When this attribute is not present, no mock-related code is generated.</remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public class GenerateModelMocksAttribute : Attribute
{
	/// <summary>
	/// Gets or sets a bool which indicates if the generation of mock factories is enabled or not.
	/// </summary>
	public bool IsEnabled { get; init; } = true;

	/// <summary>
	/// The regex patterns that a model's full name has to match (unanchored, i.e. substring semantics —
	/// anchor with '$' for an exact match) to get mock factories generated. Defaults to all generated models.
	/// </summary>
	public string[] Patterns { get; } = { ".*" };

	/// <summary>
	/// Creates a new instance using default values.
	/// </summary>
	public GenerateModelMocksAttribute()
	{
	}

	/// <summary>
	/// Creates a new instance specifying the <see cref="Patterns"/>.
	/// </summary>
	/// <param name="patterns">The patterns that a model's FullName has to match to get mock factories generated.</param>
	public GenerateModelMocksAttribute(params string[] patterns)
	{
		Patterns = patterns;
	}
}
