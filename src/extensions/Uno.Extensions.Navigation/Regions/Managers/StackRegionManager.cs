using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation.Regions.Managers;

public abstract class StackRegionManager<TControl> : BaseRegionManager<TControl>
    where TControl : class
{
    private IList<NavigationContext> NavigationContexts { get; } = new List<NavigationContext>();

    public override NavigationContext CurrentContext => NavigationContexts.LastOrDefault();

    protected StackRegionManager(
        ILogger logger,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        IDialogFactory dialogFactory,
        TControl control) : base(logger, navigation, viewModelManager, dialogFactory, control)
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
        var numberOfPagesToRemove = context.FramesToRemove;
        while (numberOfPagesToRemove > 0)
        {
            // Don't remove the last context, as that's the current page
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Now remove the current context
        NavigationContexts.RemoveAt(NavigationContexts.Count - 1);

        // Invoke the navigation (which will be a back navigation)
        GoBack(CurrentContext.Mapping?.View, context.Data, CurrentContext.ViewModel());

        return Task.CompletedTask;
    }

    protected override bool CanGoBack => NavigationContexts.Count > 1;

    public override void RegionNavigate(NavigationContext context)
    {
        var numberOfPagesToRemove = context.FramesToRemove;
        // We remove 1 less here because we need to remove the current context, after the navigation is completed
        while (numberOfPagesToRemove > 1)
        {
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Add the new context to the list of contexts and then navigate away
        NavigationContexts.Add(context);
        Show(context.Path, context.Mapping?.View, context.Data, context.ViewModel());

        // If path starts with / then remove all prior pages and corresponding contexts
        if (context.PathIsRooted)
        {
            while (NavigationContexts.Count > 1)
            {
                NavigationContexts.RemoveAt(0);
            }

            ClearBackStack();
        }

        // If there were pages to remove, after navigating we need to remove
        // the page that we've navigated away from.
        if (context.FramesToRemove > 0)
        {
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            RemoveLastFromBackStack();
        }
    }

    public override string ToString()
    {
        return $"Stack({typeof(TControl).Name}) '{CurrentContext?.Path}'";
    }

    protected abstract void GoBack(Type view, object data, object viewModel);

    protected abstract void RemoveLastFromBackStack();

    protected abstract void ClearBackStack();
}
