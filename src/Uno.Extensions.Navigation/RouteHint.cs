namespace Uno.Extensions.Navigation;

internal record RouteHint
{
	public string? Route { get; init; }
	public Type? ViewModel { get; init; }
	public Type? View { get; init; }
	public Type? Data { get; init; }
	public Type? Result { get; set; }
	public string? Qualifier { get; set; }
}
