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
        var regionContainer = new RegionService(regionLogger, services);
        //services.GetService<ScopedServiceHost<IRegionServiceContainer>>().Service = regionContainer;
        var navLogger = services.GetService<ILogger<NavigationService>>();
        var navService = new NavigationService(navLogger, services, true);
        navService.Region = regionContainer;
        services.GetService<ScopedServiceHost<INavigationRegionService>>().Service = navService;
        Root = new NavigationRegionContainer(navService, regionContainer);//  services.GetService<INavigationRegionContainer>();
    }

    public INavigationRegionContainer CreateRegion(object control, object contentControl)
    {
        Logger.LazyLogDebug(() => $"Adding region with control of type '{control.GetType().Name}'");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        // Create Navigation Service
        var navLogger = services.GetService<ILogger<NavigationService>>();
        var mappings = services.GetService<INavigationMappings>();
        var navService = new NavigationService(navLogger, services, false);
        services.GetService<ScopedServiceHost<INavigationRegionService>>().Service = navService;

        // Create Region Service Container
        var regionLogger = services.GetService<ILogger<RegionService>>();
        var regionContainer = new RegionService(regionLogger, services);
        services.GetService<ScopedServiceHost<IRegionServiceContainer>>().Service = regionContainer;

        // Associate Region Service Container with Navigation Service
        navService.Region = regionContainer;

        // Create Region Service
        services.GetService<RegionControlProvider>().RegionControl = contentControl is null ? control : (control, contentControl);
        var factory = FindFactoryForControl(control, contentControl);
        var region = factory.Create(services);
        services.GetService<ScopedServiceHost<IRegionManager>>().Service = region;

        // Associate region service with region service container
        regionContainer.Region = region;

        // Retrieve the region container and the navigation service
        return services.GetService<INavigationRegionContainer>();
    }

    private void LogAllRegions()
    {
        Logger.LazyLogInformation(() => this.ToString());
    }

    private IRegionManagerFactory FindFactoryForControl(object control, object contentControl)
    {
        var controlType = control.GetType();
        if (contentControl is null)
        {
            if (Factories.TryGetValue(controlType, out var factory))
            {
                return factory;
            }
        }

        var baseTypes = (new Type[] { controlType }).Union(controlType.GetBaseTypes()).ToArray();
        var contentBaseTypes = contentControl is not null ? (new Type[] { contentControl.GetType() }).Union(contentControl.GetType().GetBaseTypes()).ToArray() : default;
        for (int i = 0; i < baseTypes.Length; i++)
        {
            if (contentControl is not null)
            {
                for (int j = 0; j < contentBaseTypes.Length; j++)
                {
                    if (Factories.TryGetValue(typeof(ValueTuple<,>).MakeGenericType(baseTypes[i], contentBaseTypes[j]), out var baseFactory))
                    {
                        return baseFactory;
                    }
                }
            }
            else
            {
                if (Factories.TryGetValue(baseTypes[i], out var baseFactory))
                {
                    return baseFactory;
                }
            }
        }

        return null;
    }
}
