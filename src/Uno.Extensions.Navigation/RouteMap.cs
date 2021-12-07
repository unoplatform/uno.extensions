using System;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap(
	string Path,
	Type? View = null,
	bool IsDefault = false,
	Func<NavigationRequest, NavigationRequest>? Init = null,
	params RouteMap[] Nested)
{
}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

