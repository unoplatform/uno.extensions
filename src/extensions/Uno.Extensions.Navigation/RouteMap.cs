using System;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteMap(
    string Path,
    Type View = null,
    Type ViewModel = null,
    Type Data = null,
    Type ResultData = null,
    Func<IRegion, NavigationRequest, NavigationRequest> RegionInitialization = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}
