using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation.Navigators;

public class FrameNavigator : ControlNavigator<Frame>, IStackNavigator
{
	protected override FrameworkElement? CurrentView => _content;
	private FrameworkElement? _content;

	// Stores the full child route (including nested TabBar selections) for each page
	// when navigating away, so it can be restored when navigating back.
	// Key: PageStackEntry index in back stack
	private readonly Dictionary<int, Route?> _childRoutesCache = new();

	// Stores the INavigator instance (e.g. ResponseNavigator) that was active on a page
	// when navigating away from it, so it can be restored when navigating back.
	// This survives page re-creation (NavigationCacheMode=Disabled) unlike the
	// NavigatorInstance attached property which is tied to the page instance.
	// Key: PageStackEntry index in back stack
	private readonly Dictionary<int, INavigator> _navigatorCache = new();

	// Temporarily holds the child route to restore after back navigation.
	// Set during NavigatedBackAsync and consumed by AdjustRequestForChildNavigation.
	private Route? _pendingChildRoute;

	public override bool CanGoBack => Control?.BackStackDepth > 0;

	public FrameNavigator(
		ILogger<FrameNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, dispatcher, region, resolver, controlProvider.RegionControl as Frame)
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
			Control.Navigating += Frame_Navigating;
			Control.Navigated += Frame_Navigated;
		}
	}

	protected override async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		if (route.IsBackOrCloseNavigation())
		{
			// If there's a Base, then can nav forward even
			// if there's nothing on back stack, so return true
			// Otherwise, need to check logical back stack
			// If Frame.GoBack there may be no back stack but
			// there is still a page on the logical stack
			return !string.IsNullOrWhiteSpace(route.Base) || !(FullRoute?.Next()?.IsEmpty() ?? true);
		}

		if (!await base.RegionCanNavigate(route, routeMap))
		{
			return false;
		}

		if (routeMap is null)
		{
			return false;
		}

		// Can only navigate the frame to a page
		var viewType = routeMap.RenderView;
		if (
			viewType is null ||
			!viewType.IsSubclassOf(typeof(Page))
		)
		{
			return false;
		}

		return true;

	}

	protected override Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		var route = request.Route;

		// For forward navigation, capture the current child routes before clearing
		// This allows us to restore the nested state (e.g., selected TabBar item) when navigating back
		// See: https://github.com/unoplatform/uno.extensions/issues/3016
		Route? childRoute = null;
		if (route.FrameIsForwardNavigation() && Control?.BackStack is not null)
		{
			// Capture the active nested route by checking child navigators' routes
			// against the route map. We don't use Region.GetRoute() because its Merge
			// function picks children by string length, which can select the wrong
			// child when multiple FrameViews exist (e.g., picking "Second" over "Third").
			childRoute = CaptureActiveChildRoute();
		}

		// Close any active dialogs/flyouts before clearing children,
		// otherwise they persist as overlays even after navigation
		Region.CloseActiveClosableNavigators();

		// Detach all nested regions as we're moving away from the current view
		Region.Children.Clear();

		return route.FrameIsForwardNavigation() ?
					NavigateForwardAsync(request, childRoute) :
					NavigatedBackAsync(request);
	}

	private async Task<Route?> NavigateForwardAsync(NavigationRequest request, Route? previousChildRoute = null)
	{
		if (Control is null)
		{
			return default;
		}

		var route = request.Route;
		var segments = route.ForwardSegments(Resolver, this);

		// As this is a forward navigation
		if (segments.Length == 0)
		{
			Control.ReassignRegionParent();
			if (!(this.Route?.IsEmpty() ?? true) && (route.Data?.Any() ?? false))
			{
				var mapping = Resolver.FindByPath(this.Route!.Base);

				await InitializeCurrentView(request, this.Route with { Data = request.Route.Data }, mapping, true);
			}
			return request.Route;
		}


		var numberOfPagesToRemove = route.FrameNumberOfPagesToRemove();
		// We remove 1 less here because we need to remove the current context, after the navigation is completed
		while (numberOfPagesToRemove > 1)
		{
			RemoveLastFromBackStack();
			numberOfPagesToRemove--;
		}

		var lastMap = segments.Last();
		var refreshViewModel = false;


		// Need to navigate the underlying frame if it's not already
		// displaying the correct page
		if (Control!.SourcePageType != lastMap.RenderView)
		{
			// Capture the navigator of the page we're about to leave (before Frame.Navigate
			// pushes it onto the back stack). This is needed because when navigating back,
			// the Frame may create a new page instance (NavigationCacheMode=Disabled),
			// losing the NavigatorInstance attached property on the old instance.
			var previousNavigator = CurrentView?.GetNavigatorInstance();

			await Show(lastMap.Path, lastMap.RenderView, request.Route.NavigationData());

			if (Control.BackStack.Count > 0)
			{
				var backStackIndex = Control.BackStack.Count - 1;

				// Store the child route for the page we just navigated away from
				// This allows us to restore nested state (e.g., TabBar selection) when navigating back
				// The entry was just added to the back stack by Frame.Navigate
				if (previousChildRoute is not null)
				{
					_childRoutesCache[backStackIndex] = previousChildRoute;
				}

				// Store the navigator of the page we just left in a cache indexed by
				// back stack position. This ensures it can be restored on back navigation
				// even if the page instance is re-created.
				if (previousNavigator is not null)
				{
					_navigatorCache[backStackIndex] = previousNavigator;
				}
			}
		}
		else
		{
			_content = Control.Content as FrameworkElement;

			// Rebuild the nested region hierarchy
			Control.ReassignRegionParent();
			if (segments.Length > 1 ||
				!string.IsNullOrWhiteSpace(request.Route.Path) ||
				request.Route.Data?.Count > 0 ||
				request.Route.IsClearBackstack())
			{
				refreshViewModel = true;
			}
		}


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

		var parentRegion = this.Region.Parent;
		while (parentRegion is { } &&
			parentRegion.Name is null)
		{
			parentRegion = parentRegion.Parent;
		}

		for (var i = 0; i < segments.Length - 1; i++)
		{
			var map = segments[i];
			if (map.RenderView is null)
			{
				continue;
			}

			if (parentRegion?.Name == map.Path)
			{
				continue;
			}

			var newEntry = new PageStackEntry(
				map.RenderView,
				request.Route.NavigationData(),
				null);
			Control?.BackStack.Add(newEntry);
		}


		// Determine which segments in the initial route were consumed by this navigation
		var navSegment = Route.Empty;
		foreach (var stackEntry in Control!.BackStack)
		{
			var entryRoute = Resolver.FindByView(stackEntry.SourcePageType, this);
			if (entryRoute != null &&
				request.Route.Contains(entryRoute.Path))
			{
				navSegment = navSegment.Append(entryRoute.Path);
			}
		}
		navSegment = navSegment.Append(lastMap.Path);

		await InitializeCurrentView(request, lastMap.AsRoute() with { Data = request.Route.Data }, lastMap, refreshViewModel);

		var navToStore = Region.Navigator()!;
		CurrentView?.SetNavigatorInstance(navToStore);

		var responseRequest = navSegment with { Qualifier = route.Qualifier, Data = route.Data };

		return responseRequest;
	}

	private async Task<Route?> NavigatedBackAsync(NavigationRequest request)
	{
		if (Control is null)
		{
			return default;
		}

		var route = request.Route;

		// Retrieve the stored child route and navigator for the page we're navigating back to.
		// This allows us to restore nested state (e.g., TabBar selection) and the correct
		// INavigator instance (e.g., ResponseNavigator for chained GetDataAsync).
		//
		// There are two back-navigation paths:
		//   1. Our code calls Frame.GoBack() - the back stack entry is still present,
		//      so the stored route is at BackStack.Count - N, where N is the number
		//      of pages being removed (typically 1 for a single-step back).
		//   2. An external control (e.g., NavigationBar MainCommand) calls Frame.GoBack()
		//      before our code runs - the back stack entry was already popped, so the
		//      stored route is at BackStack.Count (the former BackStack.Count - N).
		//
		// We try the EXTERNAL index first (BackStack.Count) because in the internal case
		// there is never an entry at BackStack.Count (the current page isn't stored in the
		// cache - only back stack entries are). This prevents the internal path from
		// accidentally consuming an earlier page's cached data when the back stack was
		// already popped by an external GoBack.
		Route? storedChildRoute = null;
		INavigator? storedNavigator = null;

		// Try external path first: if GoBack was already performed externally
		// (e.g., NavigationBar MainCommand), the popped entry's former index
		// equals the current BackStack.Count.
		bool isExternalGoBack = false;
		{
			var externalBackIndex = Control.BackStack.Count;
			if (_childRoutesCache.TryGetValue(externalBackIndex, out storedChildRoute))
			{
				_childRoutesCache.Remove(externalBackIndex);
				isExternalGoBack = true;
			}
			if (_navigatorCache.TryGetValue(externalBackIndex, out storedNavigator))
			{
				_navigatorCache.Remove(externalBackIndex);
				isExternalGoBack = true;
			}
		}

		// Determine how many pages will be removed as part of this back navigation.
		// We default to 1 to preserve existing behavior when the route does not
		// specify a multi-step back (e.g., "--").
		var pagesToRemove = route?.FrameNumberOfPagesToRemove() ?? 1;
		if (pagesToRemove < 1)
		{
			pagesToRemove = 1;
		}

		// Fallback: internal back path. Only used when this is NOT an external GoBack.
		// When an external GoBack pops the back stack before we run, the internal
		// indices shift down by one. If we allowed the internal fallback, it would
		// grab a child route belonging to a different (earlier) page, consuming it
		// prematurely and causing the correct page to lose its nested state.
		if (!isExternalGoBack && Control.BackStack.Count > 0)
		{
			var backStackIndex = Control.BackStack.Count - pagesToRemove;
			if (backStackIndex >= 0 && backStackIndex < Control.BackStack.Count)
			{
				if (storedChildRoute is null && _childRoutesCache.TryGetValue(backStackIndex, out storedChildRoute))
				{
					_childRoutesCache.Remove(backStackIndex);
				}
				if (storedNavigator is null && _navigatorCache.TryGetValue(backStackIndex, out storedNavigator))
				{
					_navigatorCache.Remove(backStackIndex);
				}
			}
		}

		// Remove any excess items in the back stack
		var numberOfPagesToRemove = route?.FrameNumberOfPagesToRemove() ?? 0;
		while (numberOfPagesToRemove > 0)
		{
			// Don't remove the last context, as that's the current page
			RemoveLastFromBackStack();
			numberOfPagesToRemove--;
		}
		var responseRoute = route is not null ? route with { Path = null } : Route.Empty;
		var previousRoute = FullRoute.ApplyFrameRoute(Resolver, responseRoute, this);

		// Store the child route for injection into the child navigation request.
		// AdjustRequestForChildNavigation will use this to restore TabBar selections
		// and other nested state instead of navigating to the default child route.
		if (storedChildRoute?.IsEmpty() == false)
		{
			_pendingChildRoute = storedChildRoute;
		}

		var previousBase = previousRoute?.Last()?.Base;
		var currentBases = Resolver.FindByView(Control.Content.GetType(), this);
		if (previousBase is not null)
		{
			if (
			Control.BackStack.Count > 0 &&
			currentBases?.Path != previousBase &&
			previousBase != Control.Content.GetType().Name)
			{
				var previousMapping = Resolver.FindByView(Control.BackStack.Last().SourcePageType, this);
				// Invoke the navigation (which will be a back navigation)
				if (await FrameGoBack(route?.NavigationData(), previousMapping) is { } parameter)
				{
					request = request.WithData(parameter);
					responseRoute = CloneWithData(responseRoute, request.Route.Data);
					previousRoute = CloneWithData(previousRoute, request.Route.Data);
				}
			}
			else
			{
				_content = Control.Content as FrameworkElement;
			}
		}
		else
		{
			// Attempting to navigate back when there's no previous page
			responseRoute = Route.Empty;
			_content = Control.Content as FrameworkElement;
		}

		var mapping = Resolver.FindByView(Control.Content.GetType(), this);

		// Restore the INavigator BEFORE InitializeCurrentView so that the ViewModel
		// created during initialization gets the correct navigator from DI.
		// In chained GetDataAsync scenarios (TabOne→Sibling→SiblingTwo→back→Sibling),
		// the page may be re-created (NavigationCacheMode=Disabled), so the attached
		// property is lost. The _navigatorCache survives page re-creation.
		if (storedNavigator is null)
		{
			// Fall back to the page's attached property (works when page instance is cached)
			storedNavigator = CurrentView?.GetNavigatorInstance();
		}

		if (storedNavigator is not null)
		{
			Region.Services?.AddScopedInstance<INavigator>(storedNavigator);
		}

		await InitializeCurrentView(request, previousRoute ?? Route.Empty, mapping);

		return responseRoute;
	}

	private static Route? CloneWithData(Route? route, IDictionary<string, object>? data)
	{
		if (route is { } && data is { Count:> 0})
		{
			return route with { Data = data };
		}

		return route;
	}

	private void Frame_Navigating(object sender, NavigatingCancelEventArgs e)
	{
		if (e.NavigationMode == NavigationMode.Back &&
			!e.Cancel &&
			Control?.Content is Page currentPage)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Frame has navigating to previous page");

			// Force ViewModel to be unset
			currentPage.DataContext = null;
			// Force page to be disposed
			currentPage.NavigationCacheMode = NavigationCacheMode.Disabled;
		}
	}


	private void Frame_Navigated(object sender, NavigationEventArgs e)
	{
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Frame has navigated to page '{e.SourcePageType.Name}'");

		if (e.NavigationMode == NavigationMode.New)
		{
			var viewType = Control?.Content.GetType();
			if (viewType is not null)
			{
				Region.Navigator()?.NavigateViewAsync(this, viewType, data: e.Parameter);
			}
		}
		else
		{
			if (e.Parameter is null)
			{
				Region.Navigator()?.NavigateBackAsync(this);
			}
			else
			{
				Region.Navigator()?.NavigateBackWithResultAsync(this, data: e.Parameter);
			}
		}
	}

	private async Task<object?> FrameGoBack(object? parameter, RouteInfo? previousMapping)
	{
		if (Control is null)
		{
			return default;
		}

		try
		{
			Control.Navigated -= Frame_Navigated;

			parameter ??= Control.BackStack.LastOrDefault()?.Parameter;

			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Invoking Frame.GoBack");
			Control.GoBack();

			_content = Control.Content as FrameworkElement;
			await EnsurePageLoaded(previousMapping?.Path);

			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Frame.GoBack completed");
			Control.Navigated += Frame_Navigated;
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to go back to page - {ex.Message}");
		}

		return parameter;
	}

	protected override async Task<string?> Show(
		string? path,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type? viewType,
		object? data)
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
				_content = Control.Content as FrameworkElement;

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
	}

	private void RemoveLastFromBackStack()
	{
		if (Control is null)
		{
			return;
		}
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Removing last item from backstack (current count = {Control.BackStack.Count})");
		
		// Clean up the cached child route and navigator for the entry being removed
		var indexToRemove = Control.BackStack.Count - 1;
		_childRoutesCache.Remove(indexToRemove);
		_navigatorCache.Remove(indexToRemove);
		
		Control.BackStack.RemoveAt(indexToRemove);
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Item removed from backstack");
	}

	private void ClearBackStack()
	{
		if (Control is null)
		{
			return;
		}

		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Clearing backstack");
		
		// Clear all cached child routes and navigators
		_childRoutesCache.Clear();
		_navigatorCache.Clear();
		
		Control.BackStack.Clear();
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Backstack cleared");
	}

	public Route? FullRoute { get; private set; }

	protected override void UpdateRoute(Route? route)
	{
		if (route is null)
		{
			return;
		}

		FullRoute = FullRoute.ApplyFrameRoute(Resolver, route, this);
		var lastRoute = FullRoute;
		while (lastRoute is not null &&
			!lastRoute.IsLast())
		{
			lastRoute = lastRoute.Next();
		}
		Route = lastRoute;

		if (Region.Parent?.Navigator() is PanelVisiblityNavigator pvn)
		{
			pvn.UpdateCurrentRoute(lastRoute);
		}
	}

	protected override Task CheckLoadedAsync() => _content is not null ? _content.EnsureLoaded() : Task.CompletedTask;

	/// <summary>
	/// Captures the currently active nested route by checking child navigator routes
	/// against the route map's nested routes. This avoids using Region.GetRoute() which
	/// uses a Merge function that picks children by string length and can select the
	/// wrong child when multiple FrameViews exist in a PanelVisibilityNavigator.
	/// </summary>
	private Route? CaptureActiveChildRoute()
	{
		var currentBase = Route?.Base;
		if (currentBase is null)
		{
			return null;
		}

		var routeMap = Resolver.FindByPath(currentBase);
		if (routeMap?.Nested is not { Length: > 0 } nestedRoutes)
		{
			return null;
		}

		var nestedNames = new HashSet<string>(
			nestedRoutes.Select(r => r.Path).Where(p => !string.IsNullOrWhiteSpace(p))!);
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"CaptureActiveChildRoute: Looking for active nested route among: [{string.Join(", ", nestedNames)}]");

		var activeNested = FindActiveNestedRoute(Region, nestedNames);
		if (activeNested is null)
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"CaptureActiveChildRoute: No active nested route found");
			return null;
		}

		var result = new Route(Qualifiers.None, currentBase).Append(activeNested);
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"CaptureActiveChildRoute: Captured active child route '{activeNested}' for base '{currentBase}'");
		return result;
	}

	/// <summary>
	/// Recursively walks child regions to find a navigator whose Route.Base matches
	/// one of the expected nested route names. Skips regions whose view (or any visual
	/// ancestor) is Collapsed, ensuring we only find the currently active/visible child
	/// (e.g., the selected tab's content in a PanelVisibilityNavigator).
	/// </summary>
	private string? FindActiveNestedRoute(IRegion region, HashSet<string> nestedNames)
	{
		foreach (var child in region.Children)
		{
			// Skip regions whose view is effectively collapsed.
			// PanelVisibilityNavigator keeps all FrameViews as visual children of its Grid
			// but collapses inactive ones. The Frame region inside a collapsed FrameView
			// would still have Route.Base set from previous navigation. We must skip it
			// to avoid picking a stale (inactive) tab.
			if (IsEffectivelyCollapsed(child.View))
			{
				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"FindActiveNestedRoute: Skipping collapsed region '{child.Name}'");
				continue;
			}

			var nav = child.Navigator();
			var baseName = nav?.Route?.Base;
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"FindActiveNestedRoute: child region name='{child.Name}', navigator type='{nav?.GetType().Name}', Route.Base='{baseName}'");

			if (!string.IsNullOrEmpty(baseName) && nestedNames.Contains(baseName))
			{
				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"FindActiveNestedRoute: Found match '{baseName}'");
				return baseName;
			}

			var found = FindActiveNestedRoute(child, nestedNames);
			if (found is not null)
			{
				return found;
			}
		}
		return null;
	}

	/// <summary>
	/// Checks whether the given element or any of its visual ancestors has
	/// Visibility set to Collapsed. When a parent FrameView is collapsed by
	/// PanelVisibilityNavigator, the Frame inside it is effectively hidden
	/// even though its own Visibility property may still be Visible.
	/// </summary>
	private static bool IsEffectivelyCollapsed(FrameworkElement? element)
	{
		var current = element;
		while (current is not null)
		{
			if (current.Visibility == Visibility.Collapsed)
			{
				return true;
			}
			current = VisualTreeHelper.GetParent(current) as FrameworkElement;
		}
		return false;
	}

	/// <inheritdoc />
	protected override NavigationRequest AdjustRequestForChildNavigation(NavigationRequest request)
	{
		if (_pendingChildRoute is null || _pendingChildRoute.IsEmpty())
		{
			return request;
		}

		var storedRoute = _pendingChildRoute;
		_pendingChildRoute = null;

		// Only inject the child route if the remaining request has no explicit child route.
		// Treat qualifier-only routes (e.g., multi-step back "--") as empty so we can restore
		// the cached child/TabBar state when navigating back multiple steps.
		var route = request.Route;
		var hasExplicitChildRoute = !string.IsNullOrEmpty(route.Base) || !string.IsNullOrEmpty(route.Path);
		if (hasExplicitChildRoute)
		{
			return request;
		}

		// Extract the child portion of the stored route.
		// The stored route is the full region route (e.g., "Main/Third"),
		// so we skip segments until we get past this navigator's own route base.
		var childRoute = storedRoute;
		if (childRoute.Base == Route?.Base)
		{
			childRoute = childRoute.Next();
		}

		if (childRoute.IsEmpty())
		{
			return request;
		}

		// Set qualifier to None so it's treated as a fresh forward navigation
		// to the child region, not as a nested qualifier or back navigation.
		childRoute = childRoute with { Qualifier = Qualifiers.None };

		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Restoring previously selected child route '{childRoute.Base}' after back navigation");

		// Strip Result from child restoration requests. The Result type is only meaningful
		// for the original navigate-for-result flow and should not leak into child region
		// restoration. Without this, child navigators would create unnecessary
		// ResponseNavigators that corrupt navigator scope registrations.
		return request with { Route = childRoute, Result = null };
	}
}
