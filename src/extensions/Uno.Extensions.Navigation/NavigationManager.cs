﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Regions.Managers;

namespace Uno.Extensions.Navigation;

public class NavigationManager : INavigationManager
{
    public IRegion Root { get; }

    private IServiceProvider Services { get; }

    private IDictionary<Type, IRegionManagerFactory> Factories { get; }

    private ILogger Logger { get; }

    public NavigationManager(ILogger<NavigationManager> logger, IServiceProvider services, IEnumerable<IRegionManagerFactory> factories)
    {
        Logger = logger;
        Services = services;
        Factories = factories.ToDictionary(x => x.ControlType);

        // Create root navigation service
        var navLogger = services.GetService<ILogger<NavigationService>>();
        var navService = new NavigationService(navLogger, services, true);

        // Create root region service
        var regionLogger = services.GetService<ILogger<Region>>();
        var region = new Region(regionLogger, services, null, navService);

        // Associate region and nav services and set as Root
        navService.Region = region;
        services.GetService<ScopedServiceHost<INavigationService>>().Service = navService;
        Root = region;
    }

    public IRegion CreateService(IRegion parent, params object[] controls)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        // Create Navigation Service
        var navLogger = services.GetService<ILogger<NavigationService>>();
        var navService = new NavigationService(navLogger, services, false);
        services.GetService<ScopedServiceHost<INavigationService>>().Service = navService;

        // Create Region Service Container
        var regionLogger = services.GetService<ILogger<Region>>();
        var region = new Region(regionLogger, services, parent, navService);
        services.GetService<ScopedServiceHost<IRegion>>().Service = region;

        // Associate Region Service Container with Navigation Service
        navService.Region = region;

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
                region.Manager = manager;
            }
        }

        if(composite is not null)
        {
            services.GetService<ScopedServiceHost<IRegionManager>>().Service = composite;
            // Associate region service with region service container
            region.Manager = composite;
        }

        // Retrieve the region container and the navigation service
        return region;
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
