using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class RegionNavigationServiceFactory : IRegionNavigationServiceFactory
{
    private IServiceProvider Services { get; }

    private IDictionary<Type, IRegionFactory> Factories { get; }

    private ILogger Logger { get; }

    public RegionNavigationServiceFactory(ILogger<RegionNavigationServiceFactory> logger, IServiceProvider services, IEnumerable<IRegionFactory> factories)
    {
        Logger = logger;
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

        var navService = new RegionNavigationService(navLogger, services.GetService <IDialogNavigationServiceFactory>());
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
}
