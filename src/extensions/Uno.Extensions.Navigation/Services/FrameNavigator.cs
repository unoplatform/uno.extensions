using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
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

namespace Uno.Extensions.Navigation.Services;

public class FrameNavigator : ControlNavigator<Frame>
{
    protected override FrameworkElement CurrentView => Control.Content as FrameworkElement;

    protected override bool CanGoBack => true;

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

    protected override Task<NavigationRequest> NavigateWithContextAsync(NavigationContext context)
    {
        // Detach all nested regions as we're moving away from the current view
        Region.DetachAll();

        return context.Request.Route.FrameIsForwardNavigation() ?
                    NavigateForwardAsync(context) :
                    NavigatedBackAsync(context);
    }

    private async Task<NavigationRequest> NavigateForwardAsync(NavigationContext context)
    {
        var numberOfPagesToRemove = context.Request.Route.FrameNumberOfPagesToRemove();
        // We remove 1 less here because we need to remove the current context, after the navigation is completed
        while (numberOfPagesToRemove > 1)
        {
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }

        var currentRequest = context.Request;
        var segments = (from pg in currentRequest.Route.ForwardNavigationSegments()
                        let map = Mappings.FindByPath(pg.Base)
                        select new { Route = pg, Map = map }).ToArray();

        var firstSegment = segments.First().Route;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            var seg = segments[i];
            var newEntry = new PageStackEntry(seg.Map.View, null, null);
            Control.BackStack.Add(newEntry);
            currentRequest = currentRequest with { Route = currentRequest.Route.Trim(seg.Route) };
            firstSegment = firstSegment.Append(segments[i + 1].Route);
            context = context with { Mapping = segments[i + 1].Map };
        }

        //// Add the new context to the list of contexts and then navigate away
        //await Show(context.Request.Route.Base, context.Mapping?.View, context.Request.Route.Data);

        // Add the new context to the list of contexts and then navigate away
        await Show(segments.Last().Route.Base, segments.Last().Map.View, context.Request.Route.Data);

        // If path starts with / then remove all prior pages and corresponding contexts
        if (context.Request.Route.FrameIsRooted())
        {
            ClearBackStack();
        }

        // If there were pages to remove, after navigating we need to remove
        // the page that we've navigated away from.
        if (context.Request.Route.FrameNumberOfPagesToRemove() > 0)
        {
            RemoveLastFromBackStack();
        }

        InitialiseView(context);

        var responseRequest = context.Request with { Route = firstSegment with { Scheme = context.Request.Route.Scheme } };
        return responseRequest;
    }

    private Task<NavigationRequest> NavigatedBackAsync(NavigationContext context)
    {
        // Remove any excess items in the back stack
        var numberOfPagesToRemove = context.Request.Route.FrameNumberOfPagesToRemove();
        while (numberOfPagesToRemove > 0)
        {
            // Don't remove the last context, as that's the current page
            RemoveLastFromBackStack();
            numberOfPagesToRemove--;
        }
        var responseRequest = context.Request with { Route = context.Request.Route with { Path = null } };
        var previousBase = CurrentRoute.ApplyFrameRoute(responseRequest.Route).Base;
        var currentBase = Mappings.FindByView(Control.Content.GetType())?.Path;
        if (currentBase != previousBase)
        {
            // Invoke the navigation (which will be a back navigation)
            FrameGoBack(context.Request.Route.Data);
        }

        // Back navigation doesn't have a mapping (since path is "..")
        // Now that we've completed the actual navigation we can
        // use the type of the new view to look up the mapping
        var mapping = Mappings.FindByView(CurrentView?.GetType());
        context = context with { Mapping = mapping };

        InitialiseView(context);

        return Task.FromResult(responseRequest);
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

    protected override async Task Show(string path, Type viewType, object data)
    {
        try
        {
            if (Control.Content?.GetType() != viewType)
            {
                Logger.LogDebugMessage($"Invoking Frame.Navigate to type '{viewType.Name}'");
                Control.Navigated -= Frame_Navigated;
                var nav = Control.Navigate(viewType, data);
                await (Control.Content as FrameworkElement).EnsureLoaded();
                Control.Navigated += Frame_Navigated;
                Logger.LogDebugMessage($"Frame.Navigate completed");
            }
        }
        catch (Exception ex)
        {
            Logger.LogErrorMessage($"Unable to navigate to page - {ex.Message}");
        }
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

    protected override void UpdateRouteFromRequest(NavigationRequest request)
    {
        CurrentRoute = CurrentRoute.ApplyFrameRoute(request.Route);
        //var scheme = request.Route.Scheme;
        //if (string.IsNullOrWhiteSpace(request.Route.Scheme))
        //{
        //    scheme = Schemes.NavigateForward;
        //}
        //if (CurrentRoute is null)
        //{
        //    CurrentRoute = request.Route with { Scheme = Schemes.NavigateForward };// new Route(scheme, request.Route.Base, request.Route.Path, request.Route.Data);
        //}
        //else
        //{
        //    var segments = CurrentRoute.ForwardNavigationSegments().ToList();
        //    foreach (var schemeChar in scheme)
        //    {
        //        if (schemeChar + "" == Schemes.NavigateBack)
        //        {
        //            segments.RemoveAt(segments.Count - 1);
        //        }
        //        else if (schemeChar + "" == Schemes.Root)
        //        {
        //            segments.Clear();
        //        }
        //    }

        //    var newSegments = request.Route.ForwardNavigationSegments();
        //    if (newSegments is not null)
        //    {
        //        segments.AddRange(newSegments);
        //    }

        //    var routeBase = segments.First().Base;
        //    segments.RemoveAt(0);

        //    var routePath = segments.Count > 0 ? string.Join("", segments) : string.Empty;

        //    CurrentRoute = new Route(Schemes.NavigateForward, routeBase, routePath, request.Route.Data);
        //}
    }
}
