using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Regions.Managers;

namespace Uno.Extensions.Navigation;

public class NavigationServiceFactory : INavigationServiceFactory
{
    public IRegionNavigationService Root { get; }

    private IServiceProvider Services { get; }

    private IDictionary<Type, IRegionManagerFactory> Factories { get; }

    private ILogger Logger { get; }

    public NavigationServiceFactory(ILogger<NavigationServiceFactory> logger, IServiceProvider services, IEnumerable<IRegionManagerFactory> factories)
    {
        Logger = logger;
        Services = services;
        Factories = factories.ToDictionary(x => x.ControlType);

        // Create root navigation service
        var navLogger = services.GetService<ILogger<NavigationService>>();
        var navService = new NavigationService(navLogger, services, null);

        services.GetService<ScopedServiceHost<INavigationService>>().Service = navService;
        Root = navService;
    }

    public IRegionNavigationService CreateService(IRegionNavigationService parent, params object[] controls)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        // Create Navigation Service
        var navLogger = services.GetService<ILogger<NavigationService>>();
        var navService = new NavigationService(navLogger, services, parent);
        services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service = navService;

        // Create Region Service
        controls = controls.Where(c => c is not null).ToArray();
        CompositeRegionManager composite = controls.Length > 1 ? services.GetService<CompositeRegionManager>() : default;
        foreach (var control in controls)
        {
            services.GetService<RegionControlProvider>().RegionControl = control;
            var factory = FindFactoryForControl(control);
            var manager = factory.Create(services);
            if (composite is not null)
            {
                composite.Regions.Add(manager);
            }
            else
            {
                services.GetService<ScopedServiceHost<IRegionManager>>().Service = manager;
                // Associate region service with region service container
                navService.Manager = manager;
            }
        }

        if (composite is not null)
        {
            services.GetService<ScopedServiceHost<IRegionManager>>().Service = composite;
            // Associate region service with region service container
            navService.Manager = composite;
        }

        // Retrieve the region container and the navigation service
        return navService;
    }

    private IRegionManagerFactory FindFactoryForControl(object control)
    {
        var controlType = control.GetType();
        if (Factories.TryGetValue(controlType, out var factory))
        {
            return factory;
        }

        var baseTypes = controlType.GetBaseTypes().ToArray();
        for (var i = 0; i < baseTypes.Length; i++)
        {
            if (Factories.TryGetValue(baseTypes[i], out var baseFactory))
            {
                return baseFactory;
            }
        }

        return null;
    }
}
