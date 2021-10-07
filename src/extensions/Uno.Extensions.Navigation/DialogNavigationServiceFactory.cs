using System;
using System.Collections.Generic;
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

    private IEnumerable<IDialogManager> DialogProviders { get; }

    public DialogNavigationServiceFactory(ILogger<RegionNavigationServiceFactory> logger, IServiceProvider services, IEnumerable<IDialogManager> dialogProviders)
    {
        Logger = logger;
        Services = services;
        DialogProviders = dialogProviders; 
    }

    public IRegionNavigationService CreateService(NavigationRequest request)
    {
        Logger.LazyLogDebug(() => $"Adding region");

        var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        var dialogNavService = services.GetService<DialogNavigationService>();
        var dialogRegion = services.GetService<DialogRegion>();
        foreach (var dlg in DialogProviders)
        {
            if (dlg.IsDialogNavigation(request))
            {
                dialogRegion.DialogProvider = dlg;
                break;
            }
        }
        dialogNavService.Region = dialogRegion;

        return dialogNavService;
    }

}
