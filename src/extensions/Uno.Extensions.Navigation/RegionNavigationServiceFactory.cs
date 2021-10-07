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
    public IRegionNavigationService Root { get; }

    private IServiceProvider Services { get; }

    private IDictionary<Type, IRegionFactory> Factories { get; }

    private ILogger Logger { get; }

    public RegionNavigationServiceFactory(ILogger<RegionNavigationServiceFactory> logger, IServiceProvider services, IEnumerable<IRegionFactory> factories)
    {
        Logger = logger;
        Services = services;
        Factories = factories.ToDictionary(x => x.ControlType);

        // Create root navigation service
        var navLogger = services.GetService<ILogger<RegionNavigationService>>();
        var dialogFactory = services.GetService<IDialogFactory>();
        var navService = new RegionNavigationService(navLogger, dialogFactory);

        services.GetService<ScopedServiceHost<INavigationService>>().Service = navService;
        Root = navService;

        // Create a special nested service which is used to display dialogs
        var dialogService = CreateService(null, false);
        Root.Attach(RouteConstants.RelativePath.DialogPrefix, dialogService);
    }

    public IRegionNavigationService CreateService(object control, bool isComposite)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        // Create Navigation Service
        var navLogger = services.GetService<ILogger<RegionNavigationService>>();
        var dialogFactory = services.GetService<IDialogFactory>();

        if (isComposite)
        {
            var compService = new CompositeNavigationService(navLogger, dialogFactory);
            services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service = compService;
            return compService;
        }

        var navService = new RegionNavigationService(navLogger, dialogFactory);
        services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service = navService;

        // Create Region Service
        if (control is null)
        {
            navService.Region = services.GetService<DialogRegion>();
            return navService;
        }

        services.GetService<RegionControlProvider>().RegionControl = control;
        var factory = FindFactoryForControl(control);
        var region = factory.Create(services);
        services.GetService<ScopedServiceHost<IRegion>>().Service = region;
        // Associate region service with region service container
        navService.Region = region;

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
