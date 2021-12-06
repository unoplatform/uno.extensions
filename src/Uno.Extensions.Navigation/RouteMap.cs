using System;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap(
	string Path,
	Type? View = null,
	Func<NavigationRequest, NavigationRequest>? Init = null)
{
}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

