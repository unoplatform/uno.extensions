using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Navigation;

public class RouteMappings : IRouteMappings
{
    private IDictionary<string, RouteMap> Mappings { get; } = new Dictionary<string, RouteMap>();

    private ILogger Logger { get; }

    protected RouteMappings(ILogger logger)
    {
        Logger = logger;
    }

    public RouteMappings(ILogger<RouteMappings> logger)
    {
        Logger = logger;
    }

    public void Register(RouteMap map)
    {
        Mappings[map.Path] = map;
    }

    public RouteMap? Find(Route route) => FindByPath(route.Base);

    public virtual RouteMap? FindByPath(string? path)
    {
        if (
            path is null ||
            string.IsNullOrWhiteSpace(path) ||
            path == Schemes.Parent ||
            path == Schemes.Current)
        {
            return null;
        }

        return Mappings.TryGetValue(path, out var map) ? map : default;
    }

    public virtual RouteMap? FindByViewModel(Type? viewModelType)
    {
        return FindByInheritedTypes(viewModelType, map => map.ViewModel);
    }

    public virtual RouteMap? FindByView(Type? viewType)
    {
        return FindByInheritedTypes(viewType, map => map.View);
    }

    public virtual RouteMap? FindByData(Type? dataType)
    {
        return FindByInheritedTypes(dataType, map => map.Data);
    }

    public virtual RouteMap? FindByResultData(Type? dataType)
    {
        return FindByInheritedTypes(dataType, map => map.ResultData);
    }

    private RouteMap? FindByInheritedTypes(Type? typeToFind, Func<RouteMap, Type?> mapType)
    {
        // Handle the non-reflection check first
        var map = (from m in Mappings
                   where mapType(m.Value) == typeToFind
                   select m.Value)
                   .FirstOrDefault();
        if (map is not null)
        {
            return map;
        }

        return (from baseType in typeToFind.GetBaseTypes()
                from m in Mappings
                where mapType(m.Value) == baseType
                select m.Value)
                   .FirstOrDefault();
    }
}
