using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation;

internal record RouteHint
{
	public string? Route { get; init; }

	[DynamicallyAccessedMembers(Uno.Extensions.Diagnostics.Annotations.ViewModelRequirements)]
	public Type? ViewModel { get; init; }
	public Type? View { get; init; }
	public Type? Result { get; set; }
	public string? Qualifier { get; set; }
}
