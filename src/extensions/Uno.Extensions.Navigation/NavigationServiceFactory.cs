using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Regions.Managers;

namespace Uno.Extensions.Navigation;

public class NavigationServiceFactory : INavigationServiceFactory
{
    public IRegionNavigationService Root { get; }

    private IServiceProvider Services { get; }

    private IDictionary<Type, IRegionFactory> Factories { get; }

    private ILogger Logger { get; }

    public NavigationServiceFactory(ILogger<NavigationServiceFactory> logger, IServiceProvider services, IEnumerable<IRegionFactory> factories)
    {
        Logger = logger;
        Services = services;
        Factories = factories.ToDictionary(x => x.ControlType);

        // Create root navigation service
        var navLogger = services.GetService<ILogger<RegionNavigationService>>();
        var dialogFactory = services.GetService<IDialogFactory>();
        var navService = new RegionNavigationService(navLogger, null, dialogFactory);

        services.GetService<ScopedServiceHost<INavigationService>>().Service = navService;
        Root = navService;

        // Create a special nested service which is used to display dialogs
        var dialogService = CreateService(Root);
        Root.Attach(RouteConstants.RelativePath.DialogPrefix, dialogService);
    }

    public IRegionNavigationService CreateService(IRegionNavigationService parent, params object[] controls)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        // Create Navigation Service
        var navLogger = services.GetService<ILogger<RegionNavigationService>>();
        var dialogFactory = services.GetService<IDialogFactory>();
        var navService = new RegionNavigationService(navLogger, parent, dialogFactory);
        services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service = navService;

        // Create Region Service
        controls = controls.Where(c => c is not null).ToArray();

        if (!controls.Any())
        {
            navService.Region = services.GetService<DialogRegion>();
            return navService;
        }

        CompositeRegion composite = controls.Length > 1 ? services.GetService<CompositeRegion>() : default;
        foreach (var control in controls)
        {
            services.GetService<RegionControlProvider>().RegionControl = control;
            var factory = FindFactoryForControl(control);
            var region = factory.Create(services);
            if (composite is not null)
            {
                composite.Regions.Add(region);
            }
            else
            {
                services.GetService<ScopedServiceHost<IRegion>>().Service = region;
                // Associate region service with region service container
                navService.Region = region;
            }
        }

        if (composite is not null)
        {
            services.GetService<ScopedServiceHost<IRegion>>().Service = composite;
            // Associate region service with region service container
            navService.Region = composite;
        }

        // Retrieve the region container and the navigation service
        return navService;
    }

    private IRegionFactory FindFactoryForControl(object control)
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
