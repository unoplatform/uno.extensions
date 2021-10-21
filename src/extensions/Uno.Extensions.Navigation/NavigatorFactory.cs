﻿using System;
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

public class NavigatorFactoryBuilder
{
    public Action<INavigatorFactory> Configure { get; set; }
}

public class NavigatorFactory : INavigatorFactory
{
    public IDictionary<string, Type> ServiceTypes { get; } = new Dictionary<string, Type>();

    private ILogger Logger { get; }

    private IRouteMappings Mappings { get; }

    public NavigatorFactory(
        ILogger<NavigatorFactory> logger,
        IEnumerable<NavigatorFactoryBuilder> builders,
        IRouteMappings mappings)
    {
        Logger = logger;
        Mappings = mappings;
        builders.ForEach(builder => builder.Configure(this));
    }

    public void RegisterNavigator<TNavigator>(params string[] names)
        where TNavigator : INavigator
    {
        foreach (var name in names)
        {
            ServiceTypes[name] = typeof(TNavigator);
        }
    }

    public INavigator CreateService(IRegion region)
    {
        // TODO: Review creation of scoped
        Logger.LogDebugMessage($"Adding region");

        var services = region.Services;
        var control = region.View;

        // Create Navigation Service
        var navLogger = services.GetService<ILogger<ControlNavigator>>();

        INavigator navService = null;

        if (control is not null)
        {
            services.GetService<RegionControlProvider>().RegionControl = control;
            if (ServiceTypes.TryGetValue(control.GetType().Name, out var serviceType))
            {
                navService = services.GetService(serviceType) as INavigator;
            }
        }

        if (navService is null)
        {
            navService = services.GetService<Navigator>();
        }

        // Make sure the nav service gets added to the container before initialize
        // is invoked to prevent reentry
        services.AddInstance<INavigator>(navService);

        if (navService is ControlNavigator controlService)
        {
            controlService.ControlInitialize();
        }

        // Retrieve the region container and the navigation service
        return navService;
    }

    public INavigator CreateService(IRegion region, NavigationRequest request)
    {
        Logger.LogDebugMessage($"Adding region");

        // TODO: Review creation of scoped
        var scope = region.Services.CreateScope();
        var services = scope.ServiceProvider;

        var dialogRegion = new NavigationRegion(null, services);
        services.AddInstance<IRegion>(dialogRegion);

        var mapping = Mappings.FindByPath(request.Route.Base);
        var serviceType = this.FindServiceByType(mapping.View);//  ServiceTypes[mapping.View.Name];
        if (serviceType is null)
        {
            return null;
        }

        var navService = services.GetService(serviceType) as INavigator;

        services.AddInstance<INavigator>(navService);

        return navService;
    }
}
