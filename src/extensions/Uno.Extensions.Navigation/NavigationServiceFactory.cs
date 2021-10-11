using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class NavigationServiceFactory : IRegionNavigationServiceFactory, IDynamicNavigationServiceFactory
{
    private IServiceProvider Services { get; }

    private IDictionary<Type, IRegionFactory> Factories { get; }

    private ILogger Logger { get; }

    private IRouteMappings Mappings { get; }

    public NavigationServiceFactory(
        ILogger<NavigationServiceFactory> logger,
        IServiceProvider services,
        IRouteMappings mappings,
        IEnumerable<IRegionFactory> factories)
    {
        Logger = logger;
        Mappings = mappings;
        Services = services;
        Factories = factories.ToDictionary(x => x.ControlType);
    }

    public IRegionNavigationService CreateService(object control, bool isComposite)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        // Create Navigation Service
        var navLogger = services.GetService<ILogger<RegionNavigationService>>();

        if (isComposite)
        {
            var compService = new CompositeNavigationService(navLogger);
            services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service = compService;
            return compService;
        }

        var navService = new RegionNavigationService(navLogger, services.GetService<IDynamicNavigationServiceFactory>());
        services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service = navService;

        // This is a root navigation service
        if (control is null)
        {
            //navService.Region = services.GetService<DialogRegion>();
            return navService;
        }

        services.GetService<RegionControlProvider>().RegionControl = control;
        var factory = Factories.FindForControl(control);
        var region = factory.Create(services);
        services.GetService<ScopedServiceHost<IRegion>>().Service = region;
        // Associate region service with region service container
        navService.Region = region;

        // Retrieve the region container and the navigation service
        return navService;
    }

    public IRegionNavigationService CreateService(NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        var dialogNavService = services.GetService<DynamicNavigationService>();

        services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service = dialogNavService;
        var innerNavService = new InnerNavigationService(dialogNavService);
        services.GetService<ScopedServiceHost<INavigationService>>().Service = innerNavService;

        var mapping = Mappings.LookupByPath(request.Segments.Base);

        var factory = Factories.FindForControlType(mapping.View);
        var region = factory.Create(services);
        services.GetService<ScopedServiceHost<IRegion>>().Service = region;
        dialogNavService.Region = region;

        return dialogNavService;
    }
}
