namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap(
	string Path,
	ViewMap? View = null,
	bool IsDefault = false,
	string DependsOn = "",
	Func<NavigationRequest, NavigationRequest>? Init = null,
	params RouteMap[] Nested)
{
}

public record DynamicRouteMap(
	string Path,
	Func<IViewResolver,ViewMap?> ViewMap = null,
	bool IsDefault = false,
	string DependsOn = "",
	Func<NavigationRequest, NavigationRequest>? Init = null,
	params RouteMap[] Nested) : RouteMap(Path,null,IsDefault,DependsOn,Init,Nested)
{
}

#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

