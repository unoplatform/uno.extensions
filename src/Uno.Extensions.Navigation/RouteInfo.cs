namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteInfo(
	string Path,
	Func<Type?>? View = null,
	object? ViewAttributes = null,
	Type? ViewModel = null,
	Type? Data = null,
	Func<object, IDictionary<string, string>>? ToQuery = null,
	Func<IServiceProvider, IDictionary<string, object>, Task<object?>>? FromQuery = null,
	Type? ResultData = null,
	bool IsDefault = false,
	string DependsOn = "",
	Func<NavigationRequest, NavigationRequest>? Init = null,
	Func<bool>? IsDialogViewType = null,
	params RouteInfo[] Nested)
{
	public RouteInfo? Parent { get; set; }
	public RouteInfo? DependsOnRoute { get; set; }

	public Type? RenderView => View?.Invoke();
	public bool IsDependent = !string.IsNullOrWhiteSpace(DependsOn);
}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
