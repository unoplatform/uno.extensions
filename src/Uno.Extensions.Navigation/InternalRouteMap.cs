namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record InternalRouteMap(
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
	params InternalRouteMap[] Nested)
{
	public Type? RenderView => View?.Invoke();

}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
