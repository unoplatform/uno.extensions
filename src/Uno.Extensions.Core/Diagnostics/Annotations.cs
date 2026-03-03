using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Diagnostics;

/// <summary>
/// Centralized location for trimmer annotations, for great consistency!
/// </summary>
internal static class Annotations
{
	internal const DynamicallyAccessedMemberTypes ViewModelRequirements =
		  DynamicallyAccessedMemberTypes.PublicConstructors
		| DynamicallyAccessedMemberTypes.PublicProperties
		;
	internal const DynamicallyAccessedMemberTypes DataRequirements =
		DynamicallyAccessedMemberTypes.PublicProperties;
	internal const DynamicallyAccessedMemberTypes ResultDataRequirements =
		DynamicallyAccessedMemberTypes.PublicProperties;
}
