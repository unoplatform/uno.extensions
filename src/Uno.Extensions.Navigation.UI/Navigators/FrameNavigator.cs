using Uno.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.Navigators;

public class FrameNavigator : ControlNavigator<Frame>
{
	protected override FrameworkElement? CurrentView => Control?.Content as FrameworkElement;

	public override bool CanGoBack => Control?.BackStackDepth > 0;

	public FrameNavigator(
		ILogger<FrameNavigator> logger,
		IRegion region,
		IResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, region, resolver, controlProvider.RegionControl as Frame)
	{
	}

	public override void ControlInitialize()
	{
		if (Control?.Content is not null)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Navigating to type '{Control.SourcePageType.Name}' (initial Content set on Frame)");
			var viewType = Control.Content.GetType();
			Region.Navigator()?.NavigateViewAsync(this, viewType);
		}

		if (Control is not null)
		{
			Control.Navigated += Frame_Navigated;
		}
	}

	protected override bool CanNavigateToRoute(Route route)
	{
		if (Control is null)
		{
			return false;
		}

		if (route.IsDialog())
		{
			return false;
		}

		if (route.IsBackOrCloseNavigation())
		{
			// Back navigation code should swallow any excess back navigations (ie when
			// there is nothing on the back stack)
			return true;
		}
		else
		{
			var rm = Resolver.Routes.FindByPath(route.Base);
			if (!route.IsInternal)
			{
				if (!string.IsNullOrWhiteSpace(rm?.DependsOn))
				{
					var dependsRM = Resolver.Routes.FindByPath(rm?.DependsOn);
					if (
						(dependsRM is not null) &&
						!(
							Control?.SourcePageType == dependsRM?.View?.View ||
							((Control?.BackStack.Any() ?? false) && Control.BackStack[0].SourcePageType == dependsRM?.View?.View)
						)
						)
					{
						return false;
					}
				}
			}
			var viewType = rm?.View?.View;
			return viewType is not null &&
				viewType.IsSubclassOf(typeof(Page));
		}
	}

	protected override Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		var route = request.Route;
		// Detach all nested regions as we're moving away from the current view
		Region.Children.Clear();

		return route.FrameIsForwardNavigation() ?
					NavigateForwardAsync(request) :
					NavigatedBackAsync(request);
	}

	private async Task<Route?> NavigateForwardAsync(NavigationRequest request)
	{
		if (Control is null)
		{
			return default;
		}

		var route = request.Route;
		var segments = (from pg in route.ForwardNavigationSegments(FullRoute, Resolver.Routes)
						let map = Resolver.Routes.FindByPath(pg.Base)
						where map?.View?.View is not null &&
								map.View.View.IsSubclassOf(typeof(Page))
						select new { Route = pg, Map = map }).ToArray();
		if (segments.Length == 0)
		{
			return default;
		}

		var firstSegment = segments.First().Route;

		var refreshViewModel = false;

		// If the first segment doesn't have a dependency, then
		// need to treat the path as if it were absolute
		if (string.IsNullOrWhiteSpace(segments[0].Map.DependsOn))
		{
			var navSegment = segments.Last();

			// Need to navigate the underlying frame if it's not already
			// displaying the correct page
			if (Control.SourcePageType != navSegment.Map.View?.View)
			{
				await Show(navSegment.Route.Base, navSegment.Map.View?.View, navSegment.Route.Data);
			}
			else
			{
				// Rebuild the nested region hierarchy
				Control.ReassignRegionParent();
				if (segments.Length >1 ||
					string.IsNullOrWhiteSpace(request.Route.Path))
				{
					refreshViewModel = true;
				}
			}

			// Now iterate through the other segments and make
			// sure the back stack is correct
			for (var i = 0; i < segments.Length - 1; i++)
			{
				var seg = segments[i];
				var entry = i < (Control?.BackStack.Count ?? 0) ? Control?.BackStack[i] : default;
				if (entry is null ||
					entry.SourcePageType != seg.Map.View?.View)
				{
						var newEntry = new PageStackEntry(seg.Map.View?.View, null, null);
						Control?.BackStack.Add(newEntry);
				}
				firstSegment = firstSegment.Append(segments[i + 1].Route);
				route = route.Trim(seg.Route);
			}

			// Trim any excess backstack values
			while (Control?.BackStack.Count > segments.Length - 1)
			{
				Control.BackStack.RemoveAt(Control.BackStack.Count - 1);
			}
		}
		else
		{
			var numberOfPagesToRemove = route.FrameNumberOfPagesToRemove();
			// We remove 1 less here because we need to remove the current context, after the navigation is completed
			while (numberOfPagesToRemove > 1)
			{
				RemoveLastFromBackStack();
				numberOfPagesToRemove--;
			}

			for (var i = 0; i < segments.Length - 1; i++)
			{
				var seg = segments[i];
				var newEntry = new PageStackEntry(seg.Map.View?.View, null, null);
				Control?.BackStack.Add(newEntry);
				route = route.Trim(seg.Route);
				firstSegment = firstSegment.Append(segments[i + 1].Route);
			}

			await Show(segments.Last().Route.Base, segments.Last().Map.View?.View, route.Data);

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
		}

		InitialiseCurrentView(request, route, Resolver.Routes.Find(route), refreshViewModel);

		CurrentView?.SetNavigatorInstance(Region.Navigator()!);

		var responseRequest = firstSegment with { Qualifier = route.Qualifier };
		return responseRequest;
	}

	private Task<Route?> NavigatedBackAsync(NavigationRequest request)
	{
		if (Control is null)
		{
			return Task.FromResult<Route?>(default);
		}

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
		var previousRoute = FullRoute.ApplyFrameRoute(Resolver, responseRoute);
		var previousBase = previousRoute?.Last()?.Base;
		var currentBase = Resolver.Routes.FindByView(Control.Content.GetType())?.Path;
		if (previousBase is not null)
		{
			if (
			Control.BackStack.Count > 0 &&
			currentBase != previousBase &&
			previousBase != Control.Content.GetType().Name)
			{
				var previousMapping = Resolver.Routes.FindByView(Control.BackStack.Last().SourcePageType);
				// Invoke the navigation (which will be a back navigation)
				FrameGoBack(route.Data, previousMapping);
			}
		}
		else
		{
			// Attempting to navigate back when there's no previous page
			responseRoute = Route.Empty;
		}

		var mapping = Resolver.Routes.FindByView(Control.Content.GetType());

		InitialiseCurrentView(request, previousRoute ?? Route.Empty, mapping);

		// Restore the INavigator instance
		var navigator = CurrentView?.GetNavigatorInstance();
		if (navigator is not null)
		{
			Region.Services?.AddInstance<INavigator>(navigator);
		}

		return Task.FromResult<Route?>(responseRoute);
	}

	private void Frame_Navigated(object sender, NavigationEventArgs e)
	{
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Frame has navigated to page '{e.SourcePageType.Name}'");

		if (e.NavigationMode == NavigationMode.New)
		{
			var viewType = Control?.Content.GetType();
			if (viewType is not null)
			{
				Region.Navigator()?.NavigateViewAsync(this, viewType);
			}
		}
		else
		{
			Region.Navigator()?.NavigateBackAsync(this);
		}
	}

	private async void FrameGoBack(object? parameter, RouteMap? previousMapping)
	{
		if (Control is null)
		{
			return;
		}

		try
		{
			Control.Navigated -= Frame_Navigated;
			if (parameter is not null)
			{
				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Replacing last backstack item to inject parameter '{parameter.GetType().Name}'");
				// If a parameter is being sent back, we need to replace
				// the last frame on the backstack with one that has the correct
				// parameter value. This value can be extracted via the OnNavigatedTo method
				var entry = Control.BackStack.Last();
				var newEntry = new PageStackEntry(entry.SourcePageType, parameter, entry.NavigationTransitionInfo);
				Control.BackStack.Remove(entry);
				Control.BackStack.Add(newEntry);
			}
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Invoking Frame.GoBack");
			Control.GoBack();

			await EnsurePageLoaded(previousMapping?.Path);

			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Frame.GoBack completed");
			Control.Navigated += Frame_Navigated;
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to go back to page - {ex.Message}");
		}
	}

	protected override async Task<string?> Show(string? path, Type? viewType, object? data)
	{
		if (Control is null || viewType is null)
		{
			return string.Empty;
		}

		Control.Navigated -= Frame_Navigated;
		try
		{
			if (Control.Content?.GetType() != viewType)
			{
				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Invoking Frame.Navigate to type '{viewType.Name}'");
				var nav = Control.Navigate(viewType, data);

				var currentPage = Control.Content as Page;
				if (currentPage is not null)
				{
					// Force new view model to be created, just in case nav cache mode is set to required
					currentPage.DataContext = null;
				}

				await EnsurePageLoaded(path);

				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Frame.Navigate completed");
			}

			return path;
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to navigate to page - {ex.Message}");
		}
		finally
		{
			Control.Navigated += Frame_Navigated;
		}

		return default;
	}

	private async Task EnsurePageLoaded(string? path)
	{
		if (Control is null)
		{
			return;
		}

		var currentPage = CurrentView as Page;
		if (currentPage is not null)
		{
			currentPage.SetName(path ?? string.Empty);
			currentPage.ReassignRegionParent();
		}

		await (Control.Content as FrameworkElement).EnsureLoaded();
	}

	private void RemoveLastFromBackStack()
	{
		if (Control is null)
		{
			return;
		}
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Removing last item from backstack (current count = {Control.BackStack.Count})");
		Control.BackStack.RemoveAt(Control.BackStack.Count - 1);
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Item removed from backstack");
	}

	private void ClearBackStack()
	{
		if (Control is null)
		{
			return;
		}

		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Clearing backstack");
		Control.BackStack.Clear();
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Backstack cleared");
	}

	private Route? FullRoute { get; set; }

	protected override void UpdateRoute(Route? route)
	{
		if (route is null)
		{
			return;
		}

		FullRoute = FullRoute.ApplyFrameRoute(Resolver, route);
		var lastRoute = FullRoute;
		while (lastRoute is not null &&
			!lastRoute.IsLast())
		{
			lastRoute = lastRoute.Next();
		}
		Route = lastRoute;
	}
}

public static class FrameNavigatorExtensions
{
	public static readonly DependencyProperty NavigatorInstanceProperty =
		DependencyProperty.RegisterAttached(
			"NavigatorInstance",
			typeof(INavigator),
			typeof(FrameNavigatorExtensions),
			new PropertyMetadata(null));

	public static void SetNavigatorInstance(this FrameworkElement element, INavigator value)
	{
		element.SetValue(NavigatorInstanceProperty, value);
	}

	public static INavigator GetNavigatorInstance(this FrameworkElement element)
	{
		return (INavigator)element.GetValue(NavigatorInstanceProperty);
	}
}
