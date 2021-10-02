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
    private IList<NavigationContext> NavigationContexts { get; } = new List<NavigationContext>();

    protected override NavigationContext CurrentContext => NavigationContexts.LastOrDefault();

    protected StackRegion(
        ILogger logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        TControl control) : base(logger, scopedServices, navigation, viewModelManager, control)
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
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Now remove the current context
        NavigationContexts.RemoveAt(NavigationContexts.Count - 1);

        // Invoke the navigation (which will be a back navigation)
        GoBack(CurrentContext.Mapping?.View, context.Components.Parameters);

        var vm = CurrentViewModel;
        if (vm is null)
        {
            // This will happen if cache mode isn't set to required
            vm = ViewModelManager.CreateViewModel(CurrentContext);

            InitialiseView(vm);
        }

        return Task.CompletedTask;
    }

    protected override bool CanGoBack => NavigationContexts.Count > 1;

    public override Task RegionNavigate(NavigationContext context)
    {
        var numberOfPagesToRemove = context.Components.NumberOfPagesToRemove;
        // We remove 1 less here because we need to remove the current context, after the navigation is completed
        while (numberOfPagesToRemove > 1)
        {
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Add the new context to the list of contexts and then navigate away
        NavigationContexts.Add(context);
        Show(context.Components.NavigationPath, context.Mapping?.View, context.Components.Parameters);

        // If path starts with / then remove all prior pages and corresponding contexts
        if (context.Components.IsRooted)
        {
            while (NavigationContexts.Count > 1)
            {
                NavigationContexts.RemoveAt(0);
            }

            ClearBackStack();
        }

        // If there were pages to remove, after navigating we need to remove
        // the page that we've navigated away from.
        if (context.Components.NumberOfPagesToRemove > 0)
        {
            NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
            RemoveLastFromBackStack();
        }
        return Task.CompletedTask;
    }

    public override string ToString()
    {
        return $"Stack({typeof(TControl).Name}) '{CurrentContext?.Components.NavigationPath}'";
    }

    protected abstract void GoBack(Type view, object data);

    protected abstract void RemoveLastFromBackStack();

    protected abstract void ClearBackStack();
}
