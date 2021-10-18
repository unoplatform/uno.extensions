using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Services;
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

    private IDictionary<Type, IControlNavigationServiceFactory> Factories { get; }

    private ILogger Logger { get; }

    private IRouteMappings Mappings { get; }

    public NavigationServiceFactory(
        ILogger<NavigationServiceFactory> logger,
        IRouteMappings mappings,
        IEnumerable<IControlNavigationServiceFactory> factories)
    {
        Logger = logger;
        Mappings = mappings;
        Factories = factories.ToDictionary(x => x.ControlType);
    }

    public INavigationService CreateService(IRegion region)
    {
        // TODO: Review creation of scoped
        Logger.LazyLogDebug(() => $"Adding region");

        var services = region.Services;
        var control = region.View;

        // Create Navigation Service
        var navLogger = services.GetService<ILogger<ControlNavigationService>>();

        INavigationService navService;

        if (control is null)
        {
            navService = services.GetService<CompositeNavigationService>();
        }
        else
        {
            services.GetService<RegionControlProvider>().RegionControl = control;
            var factory = Factories.FindForControl(control);
            navService = factory.Create(services);
        }
        //services.AddInstance<INavigationService>(navService);

        //var innerNavService = new InnerNavigationService(navService);
        if (navService is ControlNavigationService controlService)
        {
            controlService.ControlInitialize();
        }

        // Retrieve the region container and the navigation service
        return navService;
    }

    public INavigationService CreateService(IServiceProvider services, NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        // TODO: Review creation of scoped
        var scope = services.CreateScope();
        services = scope.ServiceProvider;

        var mapping = Mappings.FindByPath(request.Route.Base);

        //var factoryServices = Services.CreateScope().ServiceProvider;
        //factoryServices.AddInstance<IScopedServiceProvider>(new ScopedServiceProvider(services));

        var factory = Factories.FindForControlType(mapping.View);
        var region = factory.Create(services);
        var innerNavService = new InnerNavigationService(region);
        services.AddInstance<INavigationService>(innerNavService);

        return region;
    }
}
