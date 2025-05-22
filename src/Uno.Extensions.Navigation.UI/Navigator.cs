using Uno.Extensions.Diagnostics;

namespace Uno.Extensions.Navigation;

/// <summary>
/// Base navigator implementation (used explicitly as composite navigator)
/// </summary>
public class Navigator : INavigator, IInstance<IServiceProvider>
{
	protected ILogger Logger { get; }

	protected IRegion Region { get; }

	private IRouteUpdater RouteUpdater => Region.Services!.GetRequiredService<IRouteUpdater>();

	IServiceProvider? IInstance<IServiceProvider>.Instance => Region.Services;

	/// <inheritdoc />
	public Route? Route { get; protected set; }

	protected IRouteResolver Resolver { get; }

	internal IDispatcher Dispatcher { get; }

	public Navigator(
		ILogger<Navigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver)
		: this((ILogger)logger, dispatcher, region, resolver)
	{
	}

	protected Navigator(
		ILogger logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver)
	{
		Region = region;
		Logger = logger;
		Resolver = resolver;
		Dispatcher = dispatcher;

		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"New navigator for region {region?.Name}");
		}
	}

	/// <inheritdoc />
	public async Task<NavigationResponse?> NavigateAsync(NavigationRequest request)
	{
		RouteUpdater.StartNavigation(this, Region, request);
		NavigationResponse? response;

		if (request.Source is null)
		{
			if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformationMessage($"Starting Navigation - Navigator: {this.GetType().Name} Request: {request.Route}");
			request = request with { Source = this };
		}

		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($" Navigator: {this.GetType().Name} Request: {request.Route}");

		// Do any initialisation logic that may be
		// defined for the route - allows for
		// routes to be redirected
		request = InitializeRequest(request);

		// Redirect navigation if required
		// eg route that matches a child, should be routed to that child
		// eg route that doesn't match a page for frame nav should be sent to parent
		var redirection = await RedirectNavigateAsync(request);
		if (redirection is not null)
		{
			response = await redirection;
		}
		else
		{

			// Append Internal qualifier to avoid requests being sent back to parent
			request = request.AsInternal();

			if (request.Route.IsDialog())
			{
				// Dialogs will load a separate navigation hierarchy
				// so there's no need to route the request to child regions
				response = await DialogNavigateAsync(request);
			}
			else
			{
				// Invoke the region specific navigation
				response = await RegionNavigateAsync(request);
			}
		}
		RouteUpdater.EndNavigation(this, Region, request, response);
		return response;
	}

	private async Task<Task<NavigationResponse?>?> RedirectNavigateAsync(NavigationRequest request)
	{
		if (request.Route.IsInternal)
		{
			return default;
		}

		// If Route is Empty but has Data, attempt to use
		// data to determine route to redirect to
		if (this.RedirectForNavigationData(Resolver, request) is { } dataNavResponse)
		{
			return dataNavResponse;
		}

		// If first section of Route matches the NAme of a nested region
		// then route the request to the region
		if (await RedirectForNamedNestedRegion(request) is { } namedNavResponse)
		{
			return namedNavResponse;
		}

		// Handle ./ qualifier (Nested)
		if (await RedirectForNestedQualifier(request) is { } nestedNavResponse)
		{
			return nestedNavResponse;
		}

		// Handle ! qualifier (Dialog)
		if (request.Route.IsDialog())
		{
			return RedirectForDialogQualifier(request);
		}

		// Handle / qualifiter (Root)
		if (RedirectForRootQualifier(request) is { } rootNavResponse)
		{
			return rootNavResponse;
		}


		var rm = !string.IsNullOrWhiteSpace(request.Route.Base) ? Resolver.FindByPath(request.Route.Base) : default;

		// Handle DependsOn
		if (rm is not null &&
			await RedirectForDependsOn(request, rm) is { } dependsNavResponse)
		{
			return dependsNavResponse;
		}

		// Handle implicit forward navigation (eg navigation invoked inside of a child region that should be sent to the parent region)
		// This is only required for stack navigators, as other navigators will handle this internally
		if (rm is not null &&
			await RedirectForImplicitForwardNavigation(request, rm) is { } implicitNavResponse)
		{
			return implicitNavResponse;
		}

		// If the current navigator can handle this route,
		// then simply return without redirecting the request but
		// only if parent can't navigate to route (eg composite region
		// where request needs to be sent to parent so that all child
		// regions receive the request)
		// Required for Test: Given_NavigationView.When_NavigationView
		if (await CanNavigate(request.Route) &&
			!await ParentCanNavigate(request.Route))
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"No redirection - Navigator can handle request (and parent cannot)");
			return default;
		}


		// If this is a back/close with no other path, then return
		// as if this navigator can handl it - it can't, so the request
		// will effetively be terminated
		if (request.Route.IsBackOrCloseNavigation())
		{
			return RedirectForBackOrClose(request);
		}


		if (Region.Parent is not null)
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Redirecting unhandled request to parent");
			return Region.Parent.NavigateAsync(request);  // Required for Test: Given_NavigationView.When_NavigationView
		}
		else if (rm is not null)
		{
			return RedirectForFullRoute(request, rm);
		}

		

		return default;
	}

	private Task<NavigationResponse?>? RedirectForFullRoute(NavigationRequest request, RouteInfo rm)
	{
		var routeMaps = new List<RouteInfo> { rm };
		var parent = rm.Parent;
		while (parent is not null)
		{
			routeMaps.Insert(0, parent);
			parent = parent.Parent;
		}
		var route = new Route(Qualifiers.None, Data: request.Route.Data);
		route = BuildFullRoute(route, routeMaps);
		route = route with { Qualifier = Qualifiers.Root };
		request = request with { Route = route };

		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Building fully qualified route for unhandled request. New request: {request.Route}");

		if (Region.Navigator() is ClosableNavigator closable)
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Closing navigator and redirecting request");
			return closable.CloseAndNavigateAsync(request);
		}

		return Region.NavigateAsync(request);

	}

	private Task<NavigationResponse?>? RedirectForBackOrClose(NavigationRequest request)
	{
		// TODO: Specify test case
		if (Region.Parent is not null)
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Redirecting back navigation to parent");
			return Region.Parent.NavigateAsync(request);
		}

		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Back navigation being handled by root region");
		return default;
	}

	private async Task<Task<NavigationResponse?>?> RedirectForImplicitForwardNavigation(NavigationRequest request, RouteInfo rm)
	{
		var ancestors = Region.Ancestors(true);

		var internalRoute = request.AsInternal();

		foreach (var navAncestor in ancestors)
		{
			if (navAncestor.Navigator is IStackNavigator &&
				Resolver.FindByPath(navAncestor.Route?.Base) is {  } ancestorMap &&
				ancestorMap.Parent == rm.Parent)
			{
				return navAncestor.Navigator.NavigateAsync(internalRoute);
			}
		}

		return default;
	}

	private async Task<Task<NavigationResponse?>?> RedirectForDependsOn(NavigationRequest request, RouteInfo? rm)
	{
		if (rm?.DependsOnRoute is { } dependsOnRoute)
		{
			var ancestors = Region.Ancestors(true);

			// If
			//		route has DependsOn AND
			//		the current route equals the DependsOn value AND
			//		there is an un-named child region
			// Then
			//		route request to child region
			// Example: In commerce sample in landscape when selecting a product/deal
			// the info is presented in an unnamed contentcontrol that's located on the
			// current productlist/deallist page
			// Required for test: Given_Apps_Commerce.When_Commerce_Responsive (lanscape/wide layout)
			foreach (var ancestor in ancestors)
			{
				if (ancestor.Route?.Base != dependsOnRoute.Path)
				{
					continue;
				}

				// Iterate through the child regions and
				// look for any child regions which are unnamed
				// and that can be navigated to 
				foreach (var child in ancestor.Region.Children)
				{
					if (child.IsUnnamed(ancestor.Route) &&
						await child.CanNavigate(request.Route))
					{
						request = request.AsInternal();
						return child.NavigateAsync(request);
					}
				}
			}

			var depRequest = request.IncludeDependentRoutes(Resolver).AsInternal();
			INavigator? redirectNav = default;

			foreach (var navAncestor in ancestors)
			{
				if (navAncestor.Navigator is IStackNavigator stackNavigator &&
					navAncestor.Route is { } route &&
					route.Contains(depRequest.Route.Base!))
				{
					// Required for test: Given_Apps_ToDo.When_ToDo_Responsive
					return navAncestor.Navigator!.NavigateAsync(depRequest);
				}

				if (!(navAncestor.Navigator is null ||
					// Ignore stack navigators, as they'll
					// always be able to nav to requests with dependson
					// by adding dependson to the stack before the request
					navAncestor.Navigator is IStackNavigator) &&
					await navAncestor.Navigator.CanNavigate(depRequest.Route))
				{
					redirectNav = navAncestor.Navigator;
				}
				else if (redirectNav is not null)
				{
					if (navAncestor.Navigator?.IsComposite() ?? false)
					{
						// navAncestor is a composite region but since
						// it doesn't support navigating to the depRequest
						// we just need to reset redirectNav to null and
						// continue looking for the correct region to navigate
						redirectNav = null;
						continue;
					}

					// Required for test: Given_Apps_Commerce.When_Commerce_Responsive (portrait/narrow layout)
					return redirectNav.NavigateAsync(depRequest);
				}
			}

		}
		return default;
	}

	private Task<NavigationResponse?>? RedirectForRootQualifier(NavigationRequest request)
	{
		// / route request to root (via parent)
		//
		// Required for Test: Given_PageNavigationRegistered.When_PageNavigationRegisteredRoot
		if (request.Route.IsRoot())
		{
			if (Region.Parent?.Parent is null)
			{
				// If parent's Parent is null, then parent is the root
				// so trim the root qualifier.
				request = request with { Route = request.Route.TrimQualifier(Qualifiers.Root) with { IsInternal = true, Refresh = true } };
			}

			// If the original request came into the root navigator, then
			// need to redirect request to the same navigator with the
			// root qualifier stripped
			var region = Region.Parent is not null ? Region.Parent : Region;

			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Updating request and redirecting for root request.  New request: {request.Route}");
			return region.NavigateAsync(request);
		}

		return default;
	}

	private Task<NavigationResponse?>? RedirectForDialogQualifier(NavigationRequest request)
	{
		// ! route request to parent
		// Required for Test: Given_ContentDialog.When_SimpleContentDialog
		if (Region.Parent is not null)
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Redirecting to parent for dialog");
			return Region.Parent.NavigateAsync(request);
		}
		else
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"No redirection - at root region to handle dialog navigation request");
			return default;
		}
	}

	private async Task<Task<NavigationResponse?>?> RedirectForNamedNestedRegion(NavigationRequest request)
	{
		// Deal with any named children that match the first segment of the request
		// In this case, the request should be trimmed
		// Required for Test: Given_ContentControl.When_ContentControl
		var nested = Region.Children.Where(x => !string.IsNullOrWhiteSpace(request.Route.Base) && x.Name == request.Route.Base).ToArray();
		if (nested.Any() && !await ParentCanNavigate(request.Route))
		{
			request = request with { Route = request.Route.Next() };

			// Make sure we always include dependencies - frame navigator will
			// trim any route sections that are already in backstack
			request = request.IncludeDependentRoutes(Resolver);

			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Redirecting to children ({nested.Length}) New request: {request.Route}");
			return NavigateChildRegions(nested, request);
		}

		return default;
	}

	private async Task<Task<NavigationResponse?>?> RedirectForNestedQualifier(NavigationRequest request)
	{
		// ./ route request to nested region (named or unnamed)
		if (request.Route.IsNested())
		{
			// Nested regions (for example a frame inside a content control) aren't always loaded
			// at this point. Need to wait for the current view of this region to load to make
			// sure all nested regions are available
			// Example: Navigating to a viewmodel from ShellViewModel constructor when using a ShellView.
			// The nested FrameView won't have loaded at this point
			await EnsureChildRegionsAreLoaded();


			request = request with { Route = request.Route.TrimQualifier(Qualifiers.Nested) };

			// Make sure we always include dependencies - frame navigator will
			// trim any route sections that are already in backstack
			request = request.IncludeDependentRoutes(Resolver);

			// Send request to both unnamed children and any that have the
			// same name as the current route
			var nested = Region.Children.Where(x => string.IsNullOrWhiteSpace(x.Name) || x.Name == this.Route?.Base).ToArray();
			if (nested.Any())
			{
				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Forced redirection to children ({nested.Length}) New request: {request.Route}");
				return NavigateChildRegions(nested, request);
			}

			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Forced redirection to children but no matching child regions found");
			return Task.FromResult(default(NavigationResponse?));
		}

		return default;
	}



	private static Route BuildFullRoute(Route route, IEnumerable<RouteInfo> maps)
	{
		foreach (var map in maps)
		{
			if (route.IsEmpty())
			{
				route = route with { Base = map.Path };
			}
			else
			{
				route = route.Append(map.Path);
			}
		}

		return route;
	}


	private NavigationRequest InitializeRequest(NavigationRequest request)
	{
		// If this is an empty request on the root region
		// then look up the default route (ie for startup logic)
		if (Region.Parent is null &&
			request.Route.IsEmpty())
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Empty route on root region - looking up default route");

			// Clear any existing route information to make
			// sure the navigation is restarted
			this.Route = Route.Empty;

			// Get the first route map
			var map = Resolver.FindByPath(string.Empty);
			if (map is not null)
			{
				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Default route found with path '{map.Path}'");
				request = request with { Route = request.Route.Append(map.Path) };
			}

			// Append Internal qualifier to avoid requests being sent back to parent
			request = request.AsInternal();
		}

		var requestMap = Resolver.FindByPath(request.Route.Base);
		if (requestMap?.Init is not null)
		{
			var newRequest = requestMap.Init(request);
			while (!request.SameRouteBase(newRequest))
			{
				request = newRequest;
				requestMap = Resolver.FindByPath(request.Route.Base);
				if (requestMap?.Init is not null)
				{
					newRequest = requestMap.Init(request);
				}
			}
			request = newRequest;
		}

		return request;
	}

	/// <inheritdoc />
	public Task<bool> CanNavigate(Route route)
	{
		if (!route.IsInternal)
		{
			// Only root region should handle dialogs
			if (Region.Parent is not null)
			{
				if (route.IsDialog())
				{
					return Task.FromResult(false);
				}

			}
		}

		var routeMap = Resolver.FindByPath(route.Base);

		var canNav = RegionCanNavigate(route, routeMap);
		return canNav;
	}

	private Task<bool> ParentCanNavigate(Route route)
	{
		if (Region.Parent is null)
		{
			return Task.FromResult(false);
		}

		var parentNavigator = Region.Parent.Navigator();
		if (parentNavigator is not null &&
				(
					parentNavigator.IsComposite() ||
					// TODO: PanelVisibilityNavigator needs to be adapted to inherit from SelectorNavigator, or share an interface
					parentNavigator.GetType() == typeof(PanelVisiblityNavigator) ||
					(
						(parentNavigator.GetType().BaseType?.IsGenericType ?? false) &&
						parentNavigator.GetType().BaseType?.GetGenericTypeDefinition() == typeof(SelectorNavigator<>)
					)
				)
			)
		{
			return parentNavigator.CanNavigate(route);
		}

		return Task.FromResult(false);
	}

	protected virtual async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		// Default behaviour for all navigators is that they can't handle back or close requests
		// This is overridden by navigators that can handle close operation
		if (route.IsBackOrCloseNavigation() &&
			string.IsNullOrWhiteSpace(route.Base))
		{
			return false;
		}

		// Check type of this navigator - if base class (ie Navigator)
		// then an check children to see if they can navigate to the route
		// This won't cause a cycle as we force IsInternal to true, which will
		// avoid any parent checks in cannavigate
		// This is to support the concept of a composite region
		// whose sole responsibility is to forward navigation requests to all child regions
		if (this.IsComposite())
		{
			var internalRoute = route with { IsInternal = true };
			foreach (var child in Region.Children)
			{
				if (!await child.CanNavigate(internalRoute))
				{
					return false;
				}
			}
		}

		if (Route?.IsEmpty() == false)
		{
			var currentRouteMap = Resolver.FindByPath(Route.Base);
			if (currentRouteMap != null || routeMap != null)
			{
				if (routeMap?.Parent is not null &&
					currentRouteMap?.Parent is not null &&
					currentRouteMap?.Parent != routeMap.Parent)
				{
					return false;
				}
			}
		}

		// If a flyout is hosting a page it will inject frameview
		// resulting in an empty route for the flyout navigator.
		// In this case we need to check the child regions for a route
		// and use that to determine if the current navigator should be
		// closed (ie we can navigate to the current route inside the flyout)
		if (this is ClosableNavigator closable &&
			Route?.IsEmpty() == true &&
			Region.Children.FirstOrDefault(x => x.Navigator()?.Route?.IsEmpty() == false) is { } childRegion)
		{
			var currentRouteMap = Resolver.FindByPath(childRegion.Navigator()?.Route?.Base);
			if (currentRouteMap != null || routeMap != null)
			{
				if (routeMap?.Parent is not null &&
					currentRouteMap?.Parent is not null &&
					currentRouteMap?.Parent != routeMap.Parent)
				{
					return false;
				}
			}
		}

		return true;
	}

	private async Task<NavigationResponse?> DialogNavigateAsync(NavigationRequest request)
	{
		var dialogService = Region.Services?.GetService<INavigatorFactory>()?.CreateService(Region, request);

		// The "!" prefix is no longer required
		request = request with { Route = request.Route.TrimQualifier(Qualifiers.Dialog) };

		var dialogResponse = await (dialogService?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse?>(default));

		return dialogResponse;
	}

	private async Task<NavigationResponse?> RegionNavigateAsync(NavigationRequest request)
	{
		var services = Region.Services;
		if (services is null)
		{
			return default;
		}

		var mapping = Resolver.FindByPath(request.Route.Last().Base);

		if (mapping?.ToQuery is not null)
		{
			request = request with { Route = request.Route with { Data = request.Route.Data?.AsParameters(mapping) } };
		}

		// Setup the navigation data (eg parameters to be injected into viewmodel)
		var dataFactor = services.GetRequiredService<NavigationDataProvider>();
		dataFactor.Parameters = (request.Route?.Data) ?? new Dictionary<string, object>();

		IResponseNavigator? responseNavigator = default;
		if (request.Result is not null)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Attempting to create response navigator for result type {request.Result.Name}");
			var responseFactory = services.GetRequiredService<IResponseNavigatorFactory>();
			// Create ResponseNavigator (and register with service provider) if result is requested
			responseNavigator = request.GetResponseNavigator(responseFactory, this);
		}

		if (responseNavigator is null)
		{
			// Since this navigation isn't requesting a response, make sure
			// the current INavigator is this navigator. This will have override
			// any responsenavigator that has been registered and avoid incorrectly
			// sending a response when simply navigating back
			services.AddScopedInstance<INavigator>(this);
		}

		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Invoking control specific navigation - start");
		var executedResponse = await CoreNavigateAsync(request);
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Invoking control specific navigation - end");


		// Convert the NavigationResponse to a typed NavigationResponse where there is a response value
		// to be returned to the caller
		if (responseNavigator is not null)
		{
			return responseNavigator.AsResponseWithResult(executedResponse);
		}

		return executedResponse;

	}

	protected virtual async Task<NavigationResponse?> CoreNavigateAsync(NavigationRequest request)
	{
		try
		{
			#region Unverified
			// Don't propagate the response request further than a named region
			if (!string.IsNullOrWhiteSpace(Region.Name) && request.Result is not null)
			{
				request = request with { Result = null };
			}
			#endregion

			// Nested regions (for example a frame inside a content control) aren't always loaded
			// at this point. Need to wait for the current view of this region to load to make
			// sure all nested regions are available
			await EnsureChildRegionsAreLoaded(); // Required for Test: Given_PageNavigation.When_PageNavigationXAML



			if (Region.Children.Count == 0)
			{
				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Region has no children to forward request to");
				return default;
			}
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Region has {Region.Children.Count} children");


			// Retrieve all the navigators for the nested (child) regions
			// This needs to be done on the UI thread as it will access
			// the visual hierarchy to locate the IServiceProvider
			var navigators = await NestedNavigatorsAsync();

			if (request.Route.IsEmpty())
			{
				// Update the request to include any default routes before attempting
				// to navigate child routes
				request = DefaultRouteRequest(request, navigators);  // Required for Test: Given_ListToDetails.When_ListToDetails

				if (request.Route.IsEmpty())
				{
					return default;
				}
			}

			#region Unverified
			if (request.Route.IsBackOrCloseNavigation() && !request.Route.IsClearBackstack())
			{
				return null;
			}
			#endregion

			var children = Region.Children.Where(region =>
										// Unnamed child regions
										string.IsNullOrWhiteSpace(region.Name)   // Required for Test: Given_PageNavigation.When_PageNavigationXAML
																				 // Regions whose name matches the next route segment
										|| region.Name == request.Route.Base    // Required for Test: Given_Apps_Commerce.When_Commerce_Responsive
																				// Regions whose name matches the current route
																				// eg currently selected tab
										|| region.Name == Route?.Base // Required for Test: Given_ContentDialog.When_ComplexContentDialog
										).ToArray();
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Request is being forwarded to {children.Length} children");
			return await NavigateChildRegions(children, request);
		}
		finally
		{
			await PostNavigateAsync();
		}
	}

	protected virtual Task PostNavigateAsync() { return Task.CompletedTask; }


	private NavigationRequest DefaultRouteRequest(NavigationRequest request, (IRegion Region, INavigator Navigator)[] ChildRegions)
	{
		if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Request is empty, so need to determine if there are default routes to navigate to");

		// Check to see if there are any child regions, and if there are
		// whether there are any that don't already have a route
		if (!request.Route.Refresh &&
			ChildRegions.All(rn => !(rn.Navigator.Route?.IsEmpty() ?? true)))
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"All child regions already have a route, so don't process default routes");
			return request;
		}

		var route = Resolver.FindByPath(this.Route?.Base);
		if (route is not null)
		{
			var defaultRoute = route.Nested?.FirstOrDefault(x => x.IsDefault);
			if (defaultRoute is not null)
			{
				//if (Region.Children.FirstOrDefault(x => x.Name == defaultRoute.Path) is { } childRegion &&
				//	defaultRoute.Nested?.FirstOrDefault(x => x.IsDefault) is { } nestedDefaultRoute)
				//{
				//	request = request with { Route = request.Route.Append(nestedDefaultRoute.Path) };
				//	return await childRegion.NavigateAsync(request);
				//}

				request = request with { Route = request.Route.Append(defaultRoute.Path) };

			}
		}

		return request;
	}

	private async Task<(IRegion Region, INavigator Navigator)[]> NestedNavigatorsAsync()
		// Force navigators to be created on the UI thread before they're accessed
		=> await Dispatcher.ExecuteAsync(async cancellation =>
		{
			return (from child in Region.Children
					let nav = child.Navigator()
					select (Region: child, Navigator: nav)).ToArray();
		});



	/// <summary>
	/// This makes sure that the current view for the region
	/// is loaded, which will ensure child regions are attached
	/// Sub-classes can overide <see cref="CheckLoadedAsync">CheckLoadedAsync</see> to customise
	/// the waiting behaviour
	/// </summary>
	/// <returns></returns>
	private async Task EnsureChildRegionsAreLoaded()
	{
		var loadId = Guid.NewGuid();
		PerformanceTimer.Start(Logger, LogLevel.Trace, loadId);
		// This is required to ensure nested elements (eg Content in a ContentControl)
		// are loaded. This will ensure the Children collection is correctly populated
		await CheckLoadedAsync();
		PerformanceTimer.Stop(Logger, LogLevel.Trace, loadId);
	}

	private async Task<NavigationResponse?> NavigateChildRegions(IEnumerable<IRegion>? children, NavigationRequest request)
	{
		if (children is null)
		{
			return default;
		}

		// Append Internal qualifier to avoid requests being sent back to parent
		request = request.AsInternal();

		var tasks = new List<Task<NavigationResponse?>>();
		foreach (var child in children)
		{
			var nav = child.Navigator();
			if (nav is not null)
			{
				tasks.Add(nav.NavigateAsync(request));
			}
		}

		await Task.WhenAll(tasks);
#pragma warning disable CA1849 // We've already waited all tasks at this point (see Task.WhenAll in line above)
		return tasks.FirstOrDefault(r => r.Result is not null)?.Result;
#pragma warning restore CA1849
	}

	public override string ToString()
	{
		var current = NavigatorToString;
		if (!string.IsNullOrWhiteSpace(current))
		{
			current = $"({current})";
		}
		return $"{this.GetType().Name}{current}";
	}

	protected virtual string NavigatorToString { get; } = string.Empty;

	protected virtual Task CheckLoadedAsync()
	{
		return Task.CompletedTask;
	}
}
