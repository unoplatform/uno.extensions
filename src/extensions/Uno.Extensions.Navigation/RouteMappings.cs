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

    public virtual RouteMap FindByPath(string path)
    {
        if (path == Schemes.Parent ||
            path == Schemes.Current)
        {
            return null;
        }

        return Mappings.TryGetValue(path, out var map) ? map : default;
    }

    public virtual RouteMap FindByViewModel(Type viewModelType)
    {
        return (Mappings.FirstOrDefault(x => x.Value.ViewModel == viewModelType).Value) ?? default;
    }

    public virtual RouteMap FindByView(Type viewType)
    {
        return (Mappings.FirstOrDefault(x => x.Value.View == viewType).Value) ?? default;
    }

    public virtual RouteMap FindByData(Type dataType)
    {
        return Mappings.FirstOrDefault(x => x.Value.Data == dataType).Value;
    }

    public virtual RouteMap FindByResultData(Type dataType)
    {
        return Mappings.FirstOrDefault(x => x.Value.ResultData == dataType).Value;
    }
}
