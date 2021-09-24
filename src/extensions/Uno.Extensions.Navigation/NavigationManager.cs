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
        var navLogger = services.GetService<ILogger<NavigationService>>();
        var navService = new NavigationService(navLogger, services);
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
        var navService = new NavigationService(navLogger, services);
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
}
