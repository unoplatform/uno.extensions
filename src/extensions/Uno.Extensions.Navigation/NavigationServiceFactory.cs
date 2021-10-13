using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation;

public class NavigationServiceFactory : IRegionNavigationServiceFactory
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

    public IRegionNavigationService CreateService(IRegionNavigationService parent, object control, bool isComposite)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        (control as FrameworkElement)?.SetServiceProvider(services);

        // Create Navigation Service
        var navLogger = services.GetService<ILogger<RegionNavigationService>>();

        if (isComposite)
        {
            var compService = new CompositeNavigationService(navLogger, parent, this);
            services.AddInstance<IRegionNavigationService>(compService);
            return compService;
        }

        // This is a root navigation service
        if (control is null)
        {
            //navService.Region = services.GetService<DialogRegion>();
            var compService = new CompositeNavigationService(navLogger,parent,this);
            services.AddInstance<IRegionNavigationService>(compService);
            return compService;
        }

        var factoryServices = Services.CreateScope().ServiceProvider;
        factoryServices.GetService<RegionControlProvider>().RegionControl = control;
        factoryServices.AddInstance<IRegionNavigationService>(parent); // This will be injected as the parent of the navigation service
        factoryServices.AddInstance<IScopedServiceProvider>(new ScopedServiceProvider(services));
        var factory = Factories.FindForControl(control);
        var region = factory.Create(factoryServices);
        services.AddInstance<IRegionNavigationService>( region);
        var innerNavService = new InnerNavigationService(region);
        services.AddInstance<INavigationService>(innerNavService);

        if(region is ControlNavigationService controlService)
        {
            controlService.ControlInitialize();
        }

        // Retrieve the region container and the navigation service
        return region;
    }

    public IRegionNavigationService CreateService(NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        var mapping = Mappings.FindByPath(request.Route.Base);

        var factoryServices = Services.CreateScope().ServiceProvider;
        factoryServices.AddInstance<IScopedServiceProvider>(new ScopedServiceProvider(services));

        var factory = Factories.FindForControlType(mapping.View);
        var region = factory.Create(factoryServices);
        services.AddInstance<IRegionNavigationService>(region);

        var innerNavService = new InnerNavigationService(region);
        services.AddInstance<INavigationService>(innerNavService);

        return region;
    }
}
