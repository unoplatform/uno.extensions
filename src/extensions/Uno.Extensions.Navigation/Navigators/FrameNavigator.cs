using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
#endif

namespace Uno.Extensions.Navigation.Navigators;

public class FrameNavigator : ControlNavigator<Frame>
{
    protected override FrameworkElement? CurrentView => Control.Content as FrameworkElement;

    public override bool CanGoBack => Control.BackStackDepth > 0;

    public FrameNavigator(
        ILogger<FrameNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as Frame)
    {
    }

    public override void ControlInitialize()
    {
        if (Control.Content is not null)
        {
            Logger.LogDebugMessage($"Navigating to type '{Control.SourcePageType.Name}' (initial Content set on Frame)");
            var viewType = Control.Content.GetType();
            Region.Navigator().NavigateToViewAsync(this, viewType);
        }

        Control.Navigated += Frame_Navigated;
    }

    protected override bool CanNavigateToRoute(Route route) => base.CanNavigateToRoute(route) || route.IsFrameNavigation();

    protected override Task<Route> ExecuteRequestAsync(NavigationRequest request)
    {
        var route = request.Route;
        // Detach all nested regions as we're moving away from the current view
        Region.Children.Clear();

        return route.FrameIsForwardNavigation() ?
                    NavigateForwardAsync(request) :
                    NavigatedBackAsync(request);
    }

    private async Task<Route> NavigateForwardAsync(NavigationRequest request)
    {
        var route = request.Route;
        var numberOfPagesToRemove = route.FrameNumberOfPagesToRemove();
        // We remove 1 less here because we need to remove the current context, after the navigation is completed
        while (numberOfPagesToRemove > 1)
        {
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        var segments = (from pg in route.ForwardNavigationSegments(Mappings)
                        let map = Mappings.FindByPath(pg.Base)
                        select new { Route = pg, Map = map }).ToArray();

        var firstSegment = segments.First().Route;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var seg = segments[i];
            var newEntry = new PageStackEntry(seg.Map.View, null, null);
            Control.BackStack.Add(newEntry);
            route = route.Trim(seg.Route);
            firstSegment = firstSegment.Append(segments[i + 1].Route);
        }

        //// Add the new context to the list of contexts and then navigate away
        //await Show(context.Request.Route.Base, context.Mapping?.View, context.Request.Route.Data);

        // Add the new context to the list of contexts and then navigate away
        await Show(segments.Last().Route.Base, segments.Last().Map.View, route.Data);

        // If path starts with / then remove all prior pages and corresponding contexts
        if (route.FrameIsRooted())
        {
            ClearBackStack();
        }

        // If there were pages to remove, after navigating we need to remove
        // the page that we've navigated away from.
        if (route.FrameNumberOfPagesToRemove() > 0)
        {
            RemoveLastFromBackStack();
        }

        InitialiseCurrentView(route, Mappings.Find(route));

        var responseRequest = firstSegment with { Scheme = route.Scheme };
        return responseRequest;
    }

    protected override object InitialiseCurrentView(Route route, RouteMap? mapping)
    {


        return base.InitialiseCurrentView(route, mapping);
    }

    private Task<Route> NavigatedBackAsync(NavigationRequest request)
    {
        var route = request.Route;
        // Remove any excess items in the back stack
        var numberOfPagesToRemove = route.FrameNumberOfPagesToRemove();
        while (numberOfPagesToRemove > 0)
        {
            // Don't remove the last context, as that's the current page
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }
        var responseRoute = route with { Path = null };
        var previousRoute = FullRoute.ApplyFrameRoute(Mappings, responseRoute);
        var previousBase = previousRoute.Base;
        var currentBase = Mappings.FindByView(Control.Content.GetType())?.Path;
        if (currentBase != previousBase)
        {
            // Invoke the navigation (which will be a back navigation)
            FrameGoBack(route.Data);
        }

        // Back navigation doesn't have a mapping (since path is "..")
        // Now that we've completed the actual navigation we can
        // use the type of the new view to look up the mapping
        var mapping = Mappings.FindByView(CurrentView?.GetType());
        //context = context with { Mapping = mapping };

        InitialiseCurrentView(previousRoute, mapping);

        return Task.FromResult(responseRoute);
    }

    private void Frame_Navigated(object sender, NavigationEventArgs e)
    {
        Logger.LogDebugMessage($"Frame has navigated to page '{e.SourcePageType.Name}'");

        if (e.NavigationMode == NavigationMode.New)
        {
            var viewType = Control.Content.GetType();
            Region.Navigator().NavigateToViewAsync(this, viewType);
        }
        else
        {
            Region.Navigator().NavigateToPreviousViewAsync(this);
        }
    }

    private void FrameGoBack(object parameter)
    {
        try
        {
            Control.Navigated -= Frame_Navigated;
            if (parameter is not null)
            {
                Logger.LogDebugMessage($"Replacing last backstack item to inject parameter '{parameter.GetType().Name}'");
                // If a parameter is being sent back, we need to replace
                // the last frame on the backstack with one that has the correct
                // parameter value. This value can be extracted via the OnNavigatedTo method
                var entry = Control.BackStack.Last();
                var newEntry = new PageStackEntry(entry.SourcePageType, parameter, entry.NavigationTransitionInfo);
                Control.BackStack.Remove(entry);
                Control.BackStack.Add(newEntry);
            }

            Logger.LogDebugMessage($"Invoking Frame.GoBack");
            Control.GoBack();
            Logger.LogDebugMessage($"Frame.GoBack completed");
            Control.Navigated += Frame_Navigated;
        }
        catch (Exception ex)
        {
            Logger.LogErrorMessage($"Unable to go back to page - {ex.Message}");
        }
    }

    protected override async Task<string?> Show(string? path, Type? viewType, object? data)
    {
        Control.Navigated -= Frame_Navigated;
        try
        {
            if (Control.Content?.GetType() != viewType)
            {
                Logger.LogDebugMessage($"Invoking Frame.Navigate to type '{viewType.Name}'");
                var nav = Control.Navigate(viewType, data);

                var currentPage = CurrentView as Page;
                if (currentPage is not null)
                {
                    currentPage.SetName(path);
                }

                await (Control.Content as FrameworkElement).EnsureLoaded();
                Logger.LogDebugMessage($"Frame.Navigate completed");
            }

            return path;
        }
        catch (Exception ex)
        {
            Logger.LogErrorMessage($"Unable to navigate to page - {ex.Message}");
        }
        finally
        {
            Control.Navigated += Frame_Navigated;
        }

        return default;
    }

    private void RemoveLastFromBackStack()
    {
        Logger.LogDebugMessage($"Removing last item from backstack (current count = {Control.BackStack.Count})");
        Control.BackStack.RemoveAt(Control.BackStack.Count - 1);
        Logger.LogDebugMessage($"Item removed from backstack");
    }

    private void ClearBackStack()
    {
        Logger.LogDebugMessage($"Clearing backstack");
        Control.BackStack.Clear();
        Logger.LogDebugMessage($"Backstack cleared");
    }

    private Route FullRoute { get; set; }

    protected override void UpdateRoute(Route route)
    {
        FullRoute = FullRoute.ApplyFrameRoute(Mappings, route);
        var lastRoute = FullRoute;
        while (!lastRoute.IsLast())
        {
            lastRoute = lastRoute.Next();
        }
        Route = lastRoute;
    }
}
