namespace Uno.Extensions.Navigation.Navigators;

public class FrameNavigator : ControlNavigator<Frame>, IStackNavigator
{
	protected override FrameworkElement? CurrentView => _content;
	private FrameworkElement? _content;

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
			await Show(lastMap.Path, lastMap.RenderView, request.Route.NavigationData());
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

		CurrentView?.SetNavigatorInstance(Region.Navigator()!);

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


		// Remove any excess items in the back stack
		var numberOfPagesToRemove = route.FrameNumberOfPagesToRemove();
		while (numberOfPagesToRemove > 0)
		{
			// Don't remove the last context, as that's the current page
			RemoveLastFromBackStack();
			numberOfPagesToRemove--;
		}
		var responseRoute = route with { Path = null };
		var previousRoute = FullRoute.ApplyFrameRoute(Resolver, responseRoute, this);
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
				if (await FrameGoBack(route.NavigationData(), previousMapping) is { } parameter)
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


		await InitializeCurrentView(request, previousRoute ?? Route.Empty, mapping);


		// Restore the INavigator instance
		var navigator = CurrentView?.GetNavigatorInstance();
		if (navigator is not null)
		{
			Region.Services?.AddScopedInstance<INavigator>(navigator);
		}

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
	}

	protected override Task CheckLoadedAsync() => _content is not null ? _content.EnsureLoaded() : Task.CompletedTask;
}
