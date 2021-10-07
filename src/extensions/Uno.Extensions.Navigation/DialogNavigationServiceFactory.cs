using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class DialogNavigationServiceFactory : IDialogNavigationServiceFactory
{
    private IServiceProvider Services { get; }

    private ILogger Logger { get; }

    private IDictionary<Type, IRegionFactory> Factories { get; }

    private IRouteMappings Mappings { get; }

    public DialogNavigationServiceFactory(
        ILogger<RegionNavigationServiceFactory> logger,
        IServiceProvider services,
        IRouteMappings mappings,
        IEnumerable<IRegionFactory> factories)
    {
        Logger = logger;
        Services = services;
        Mappings = mappings;
        Factories = factories.ToDictionary(x => x.ControlType);
    }

    public IRegionNavigationService CreateService(NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        var dialogNavService = services.GetService<DialogNavigationService>();

        var mapping = Mappings.LookupByPath(request.Segments.Base);

        var factory = Factories.FindForControlType(mapping.View);
        var region = factory.Create(services);
        services.GetService<ScopedServiceHost<IRegion>>().Service = region;
        dialogNavService.Region = region;

        return dialogNavService;
    }

}
