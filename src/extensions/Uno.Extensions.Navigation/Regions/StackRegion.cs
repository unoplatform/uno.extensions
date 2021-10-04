using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation.Regions;

public abstract class StackRegion<TControl> : BaseRegion<TControl>
    where TControl : class
{
    protected StackRegion(
        ILogger logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        TControl control) : base(logger, scopedServices, navigation, viewModelManager, mappings, control)
    {
    }

    protected override Task DoNavigation(NavigationContext context)
    {
        if (context.IsBackNavigation)
        {
            return DoBackNavigation(context);
        }
        else
        {
            return DoForwardNavigation(context);
        }
    }

    protected Task DoBackNavigation(NavigationContext context)
    {
        // Remove any excess items in the back stack
        var numberOfPagesToRemove = context.Components.NumberOfPagesToRemove;
        while (numberOfPagesToRemove > 0)
        {
            // Don't remove the last context, as that's the current page
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Invoke the navigation (which will be a back navigation)
        GoBack(context.Components.Parameters);

        // Back navigation doesn't have a mapping (since path is "..")
        // Now that we've completed the actual navigation we can
        // use the type of the new view to look up the mapping
        var mapping = Mappings.LookupByView(CurrentView?.GetType());
        context = context with { Mapping = mapping };

        InitialiseView(context);

        return Task.CompletedTask;
    }

    protected override bool CanGoBack => true;

    public override Task RegionNavigate(NavigationContext context)
    {
        var numberOfPagesToRemove = context.Components.NumberOfPagesToRemove;
        // We remove 1 less here because we need to remove the current context, after the navigation is completed
        while (numberOfPagesToRemove > 1)
        {
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Add the new context to the list of contexts and then navigate away
        Show(context.Components.NavigationPath, context.Mapping?.View, context.Components.Parameters);

        // If path starts with / then remove all prior pages and corresponding contexts
        if (context.Components.IsRooted)
        {
            ClearBackStack();
        }

        // If there were pages to remove, after navigating we need to remove
        // the page that we've navigated away from.
        if (context.Components.NumberOfPagesToRemove > 0)
        {
            RemoveLastFromBackStack();
        }
        return Task.CompletedTask;
    }

    protected abstract void GoBack(object data);

    protected abstract void RemoveLastFromBackStack();

    protected abstract void ClearBackStack();
}
