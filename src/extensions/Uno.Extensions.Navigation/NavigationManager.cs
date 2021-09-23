using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class NavigationManager : INavigationManager
{
    public INavigationRegionContainer Root { get; }

    private IServiceProvider Services { get; }

    private IDictionary<Type, IRegionManagerFactory> Factories { get; }

    private ILogger Logger { get; }

    public NavigationManager(ILogger<NavigationManager> logger, IServiceProvider services, IEnumerable<IRegionManagerFactory> factories)
    {
        Logger = logger;
        Services = services;
        Factories = factories.ToDictionary(x => x.ControlType);
        var regionLogger = services.GetService<ILogger<RegionService>>();
        var regionContainer = new RegionService(regionLogger, services, null);
        //services.GetService<ScopedServiceHost<IRegionServiceContainer>>().Service = regionContainer;
        var navService = new NavigationService(null, services, null);
        navService.Region = regionContainer;
        services.GetService<ScopedServiceHost<INavigationRegionService>>().Service = navService;
        Root = new NavigationRegionContainer(navService, regionContainer);//  services.GetService<INavigationRegionContainer>();
    }

    public INavigationRegionContainer CreateRegion(object control)
    {
        Logger.LazyLogDebug(() => $"Adding region with control of type '{control.GetType().Name}'");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        var navLogger = services.GetService<ILogger<NavigationService>>();
        var mappings = services.GetService<INavigationMappings>();
        var navService = new NavigationService(navLogger, services, mappings);//, regionContainer);
        services.GetService<ScopedServiceHost<INavigationRegionService>>().Service = navService;

        // Make the control available via DI
        services.GetService<RegionControlProvider>().RegionControl = control;

        var factory = FindFactoryForControl(control);
        var region = factory.Create(services);
        services.GetService<ScopedServiceHost<IRegionManager>>().Service = region;


        var regionLogger = services.GetService<ILogger<RegionService>>();

        var regionContainer = new RegionService(regionLogger,services, region);
        services.GetService<ScopedServiceHost<IRegionServiceContainer>>().Service = regionContainer;
        navService.Region = regionContainer;

        // Retrieve the region container and the navigation service
        return services.GetService<INavigationRegionContainer>();
    }

    //public INavigationService AddRegion(INavigationService parentRegion, string regionName, object control, INavigationService existingRegion)
    //{
    //    Logger.LazyLogDebug(() => $"Adding region with control of type '{control.GetType().Name}' and region name '{regionName}'");
    //    var ans = existingRegion as NavigationService;
    //    var parent = parentRegion as NavigationService;

    //    // This ensures all region services have a parent. The root service
    //    // is used to cache initial navigation requests before the first
    //    // region is created
    //    if (parent is null)
    //    {
    //        parent = Root as NavigationService;
    //    }

    //    if (ans is null)
    //    {
    //        var scope = Services.CreateScope();
    //        var services = scope.ServiceProvider;
    //        // Make the control available via DI
    //        services.GetService<RegionControlProvider>().RegionControl = control;

    //        ans = services.GetService<INavigationService>() as NavigationService;// new NavigationService(Services.GetService<ILogger<NavigationService>>(), services, Mapping, parent);
    //        ans.Parent = parent;

    //        var factory = FindFactoryForControl(control);
    //        var region = factory.Create(services);
    //        ans.Region = region;
    //    }

    //    parent.NestedRegions[regionName + string.Empty] = ans;

    //    LogAllRegions();

    //    RunPendingNavigation(ans, parent, regionName);

    //    return ans;
    //}

    private void LogAllRegions()
    {
        Logger.LazyLogInformation(() => this.ToString());
    }

    private IRegionManagerFactory FindFactoryForControl(object control)
    {
        var controlType = control.GetType();
        if (Factories.TryGetValue(controlType, out var factory))
        {
            return factory;
        }

        var baseTypes = control.GetType().GetBaseTypes().ToArray();
        for (int i = 0; i < baseTypes.Length; i++)
        {
            if (Factories.TryGetValue(baseTypes[i], out var baseFactory))
            {
                return baseFactory;
            }
        }

        return null;
    }

    //private async Task RunPendingNavigation(NavigationService ans, NavigationService parent, string regionName)
    //{
    //    var pending = parent.PendingNavigation;
    //    parent.PendingNavigation = null;
    //    if (pending is not null)
    //    {
    //        var nextNavigationTask = pending.Value.Item1;
    //        var nextNavigation = pending.Value.Item2;

    //        if (nextNavigation.Route.Uri.OriginalString.StartsWith(regionName))
    //        {
    //            var nestedRoute = nextNavigation.Route.Uri.OriginalString.TrimStart($"{regionName}/");
    //            nextNavigation = nextNavigation with { Route = nextNavigation.Route with { Uri = new Uri(nestedRoute, UriKind.Relative) } };
    //        }
    //        await ans.NavigateAsync(nextNavigation);
    //        nextNavigationTask.SetResult(null);
    //    }
    //}

    //public void RemoveRegion(INavigationService region)
    //{
    //    var ans = region as NavigationService;
    //    if (ans is null)
    //    {
    //        Logger.LazyLogError(() => $"Unable to remove region as unable to cast to NavigationService");
    //        return;
    //    }

    //    Logger.LazyLogDebug(() => $"Removing region of type '{ans.Region.GetType().Name}'");

    //    // Detach region from parent
    //    var parent = ans.Parent as NavigationService;
    //    if (parent is not null)
    //    {
    //        parent.NestedRegions.Remove(kvp => kvp.Value == region);
    //    }

    //    LogAllRegions();
    //}

    //public NavigationResponse NavigateAsync(NavigationRequest request)
    //{
    //    return Root.NavigateAsync(request);
    //}
}
