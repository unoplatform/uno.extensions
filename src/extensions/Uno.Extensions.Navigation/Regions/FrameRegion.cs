using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.ViewModels;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
#endif

namespace Uno.Extensions.Navigation.Regions;

public class FrameRegion : ControlNavigationService<Frame>
{
    protected override object CurrentView => Control.Content;

    protected override string CurrentPath => CurrentView?.NavigationRoute(Mappings);

    public FrameRegion(
        ILogger<FrameRegion> logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory,
        IScopedServiceProvider scopedServices,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, parent, serviceFactory, scopedServices, viewModelManager, mappings, controlProvider.RegionControl as Frame)
    {
        if (Control.Content is not null)
        {
            Logger.LazyLogDebug(() => $"Navigating to type '{Control.SourcePageType.Name}' (initial Content set on Frame)");
            UpdateCurrentView();
        }

        Control.Navigated += Frame_Navigated;
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
        var numberOfPagesToRemove = context.Request.Route.FrameNumberOfPagesToRemove;
        while (numberOfPagesToRemove > 0)
        {
            // Don't remove the last context, as that's the current page
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Invoke the navigation (which will be a back navigation)
        GoBack(context.Request.Route.Data);

        // Back navigation doesn't have a mapping (since path is "..")
        // Now that we've completed the actual navigation we can
        // use the type of the new view to look up the mapping
        var mapping = Mappings.FindByView(CurrentView?.GetType());
        context = context with { Mapping = mapping };

        InitialiseView(context);

        return Task.CompletedTask;
    }

    protected override bool CanGoBack => true;

    public override Task RegionNavigate(NavigationContext context)
    {
        var numberOfPagesToRemove = context.Request.Route.FrameNumberOfPagesToRemove;
        // We remove 1 less here because we need to remove the current context, after the navigation is completed
        while (numberOfPagesToRemove > 1)
        {
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        // Add the new context to the list of contexts and then navigate away
        Show(context.Request.Route.Base, context.Mapping?.View, context.Request.Route.Data);

        // If path starts with / then remove all prior pages and corresponding contexts
        if (context.Request.Route.FrameIsRooted)
        {
            ClearBackStack();
        }

        // If there were pages to remove, after navigating we need to remove
        // the page that we've navigated away from.
        if (context.Request.Route.FrameNumberOfPagesToRemove > 0)
        {
            RemoveLastFromBackStack();
        }
        return Task.CompletedTask;
    }

    private void UpdateCurrentView()
    {
        var request = Mappings.FindByView(Control.Content.GetType()).AsRequest(this);
        var context = request.BuildNavigationContext(ScopedServices);
        InitialiseView(context);
    }

    private void Frame_Navigated(object sender, NavigationEventArgs e)
    {
        Logger.LazyLogDebug(() => $"Frame has navigated to page '{e.SourcePageType.Name}'");

        UpdateCurrentView();
    }

    private void GoBack(object parameter)
    {
        try
        {
            Control.Navigated -= Frame_Navigated;
            if (parameter is not null)
            {
                Logger.LazyLogDebug(() => $"Replacing last backstack item to inject parameter '{parameter.GetType().Name}'");
                // If a parameter is being sent back, we need to replace
                // the last frame on the backstack with one that has the correct
                // parameter value. This value can be extracted via the OnNavigatedTo method
                var entry = Control.BackStack.Last();
                var newEntry = new PageStackEntry(entry.SourcePageType, parameter, entry.NavigationTransitionInfo);
                Control.BackStack.Remove(entry);
                Control.BackStack.Add(newEntry);
            }

            Logger.LazyLogDebug(() => $"Invoking Frame.GoBack");
            Control.GoBack();
            Logger.LazyLogDebug(() => $"Frame.GoBack completed");
            Control.Navigated += Frame_Navigated;
        }
        catch (Exception ex)
        {
            Logger.LazyLogError(() => $"Unable to go back to page - {ex.Message}");
        }
    }

    protected override void Show(string path, Type view, object data)
    {
        try
        {
            if (Control.Content?.GetType() != view)
            {
                Logger.LazyLogDebug(() => $"Invoking Frame.Navigate to type '{view.Name}'");
                Control.Navigated -= Frame_Navigated;
                var nav = Control.Navigate(view, data);
                Control.Navigated += Frame_Navigated;
                Logger.LazyLogDebug(() => $"Frame.Navigate completed");
            }
        }
        catch (Exception ex)
        {
            Logger.LazyLogError(() => $"Unable to navigate to page - {ex.Message}");
        }
    }

    private void RemoveLastFromBackStack()
    {
        Logger.LazyLogDebug(() => $"Removing last item from backstack (current count = {Control.BackStack.Count})");
        Control.BackStack.RemoveAt(Control.BackStack.Count - 1);
        Logger.LazyLogDebug(() => $"Item removed from backstack");
    }

    private void ClearBackStack()
    {
        Logger.LazyLogDebug(() => $"Clearing backstack");
        Control.BackStack.Clear();
        Logger.LazyLogDebug(() => $"Backstack cleared");
    }
}
