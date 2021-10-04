using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation.Regions;

public abstract class SimpleRegion<TControl> : BaseRegion<TControl>
    where TControl : class
{
    protected SimpleRegion(
        ILogger logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        TControl control) : base(logger, scopedServices, navigation, viewModelManager, mappings, control)
    {
        Control = control;
    }

    public override Task RegionNavigate(NavigationContext context)
    {
        Logger.LazyLogDebug(() => $"Navigating to path '{context.Components.NavigationPath}' with view '{context.Mapping?.View?.Name}'");
        Show(context.Components.NavigationPath, context.Mapping?.View, context.Components.Parameters);
        return Task.CompletedTask;
    }
}
