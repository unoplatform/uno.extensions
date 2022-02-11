using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class Navigator : INavigator, IInstance<IServiceProvider>
{
	protected ILogger Logger { get; }

	protected IRegion Region { get; }

	private IRouteUpdater? RouteUpdater => Region.Services?.GetRequiredService<IRouteUpdater>();

	IServiceProvider? IInstance<IServiceProvider>.Instance => Region.Services;

	public Route? Route { get; protected set; }

	protected IRouteResolver RouteResolver { get; }

	public Navigator(ILogger<Navigator> logger, IRegion region, IRouteResolver routeResolver)
		: this((ILogger)logger, region, routeResolver)
	{
	}

	protected Navigator(ILogger logger, IRegion region, IRouteResolver routeResolver)
	{
		Region = region;
		Logger = logger;
		RouteResolver = routeResolver;
	}

	public async Task<NavigationResponse?> NavigateAsync(NavigationRequest request)
	{
		if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation($"Pre-navigation: - {Region.Root().ToString()}");
		try
		{
			RouteUpdater?.StartNavigation();

			request = InitialiseRequest(request);

			if (!request.Route.IsInternal)
			{

				if (!SchemeIsSupported(request.Route))
				{
					// Trim ../../ before routing request to parent
					if (request.Route.IsParent())
					{
						request = request with { Route = request.Route.TrimScheme(Schemes.Parent) };
					}

					if (request.Route.IsEmpty())
					{
						return default;
					}

					if (Region.Parent is not null)
					{
						return await Region.Parent.NavigateAsync(request);
					}
					else
					{
						if (Logger.IsEnabled(LogLevel.Error)) Logger.LogError($"No parent to forward request to {request}");
						return default;
					}
				}

				// Handle root navigations i.e. Scheme starts with /
				if (request.Route.IsRoot())
				{
					// Either
					// - this is the root Region, so trim Root scheme
					// - Or, this is an invalid navigation
					if (Region.Parent is null)
					{
						// This is the root nav service - need to trim the root scheme
						// so that the request can be handled by this navigator
						request = request with { Route = request.Route.TrimScheme(Schemes.Root) };
					}
					else
					{
						if (Logger.IsEnabled(LogLevel.Error)) Logger.LogError($"Attempting to handle Root scheme by non-root navigator");
						return default;
					}
				}

				// Is this region is an unnamed child of a composite,
				// send request to parent if the route has no scheme
				if (request.Route.IsChangeContent() &&
					!Region.IsNamed() &&
					Region.Parent is not null
					)
				{
					return await Region.Parent.NavigateAsync(request);
				}
			}

			if (!request.Route.IsInternal)
			{
				// Append Internal scheme to avoid requests being sent back to parent
				request = request with { Route = request.Route with { IsInternal = true } };

				// Run dialog requests
				if (request.Route.IsDialog())
				{
					return await DialogNavigateAsync(request);
				}
			}

			// Make sure the view has completely loaded before trying to process the nav request
			// Typically this might happen with the first navigation of the application where the
			// window hasn't been activated yet, so the root region may not have loaded
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug("Ensuring region has loaded - start");
			await Region.View.EnsureLoaded();
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug("Ensuring region has loaded - end");

			return await ResponseNavigateAsync(request);
		}
		finally
		{
			if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation($"Post-navigation: {Region.Root().ToString()}");
			if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation($"Post-navigation (route): {Region.Root().GetRoute()}");
			RouteUpdater?.EndNavigation();
		}
	}

	private NavigationRequest InitialiseRequest(NavigationRequest request)
	{
		var requestMap = RouteResolver.FindByPath(request.Route.Base);
		if (requestMap?.Init is not null)
		{
			var newRequest = requestMap.Init(request);
			while (!request.SameRouteBase(newRequest))
			{
				request = newRequest;
				requestMap = RouteResolver.FindByPath(request.Route.Base);
				if (requestMap?.Init is not null)
				{
					newRequest = requestMap.Init(request);
				}
			}
			request = newRequest;
		}
		return request;
	}

	protected virtual bool SchemeIsSupported(Route route) =>
		// "" (current) Has been removed to force navigation to be explicit (eg either ../ or ./)
		// route.IsCurrent() ||					

		// "./" (nested) by default all navigators should be able to forward requests
		// to child if the Base matches a named child
		(
			route.IsNested() &&
			this.Region.Children.Any(
				x => string.IsNullOrWhiteSpace(x.Name) ||
				x.Name == route.Base ||
				x.Name == this.Route?.Base)
		)
		||
		(
			// If this is root navigator, need to support / (root) and ! (dialog) requests
			(this.Region.Parent is null) &&
				(
					route.IsRoot() ||
					route.IsDialog()
				)
		);

	protected virtual bool CanNavigateToRoute(Route route) => SchemeIsSupported(route);

	private async Task<NavigationResponse?> DialogNavigateAsync(NavigationRequest request)
	{
		var dialogService = Region.Services?.GetService<INavigatorFactory>()?.CreateService(Region, request);

		var dialogResponse = await (dialogService?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse?>(default));

		return dialogResponse;
	}

	private async Task<NavigationResponse?> ResponseNavigateAsync(NavigationRequest request)
	{
		var services = Region.Services;
		if (services is null)
		{
			return default;
		}

		var mapping = RouteResolver.Find(request.Route);
		if (mapping?.UntypedToQuery is not null)
		{
			request = request with { Route = request.Route with { Data = request.Route.Data?.AsParameters(mapping) } };
		}

		// Setup the navigation data (eg parameters to be injected into viewmodel)
		var dataFactor = services.GetRequiredService<NavigationDataProvider>();
		dataFactor.Parameters = (request.Route?.Data) ?? new Dictionary<string, object>();

		var responseFactory = services.GetRequiredService<IResponseNavigatorFactory>();
		// Create ResponseNavigator if result is requested
		var navigator = request.Result is not null ? request.GetResponseNavigator(responseFactory, this) : default;

		if (navigator is null)
		{
			// Since this navigation isn't requesting a response, make sure
			// the current INavigator is this navigator. This will have override
			// any responsenavigator that has been registered and avoid incorrectly
			// sending a response when simply navigating back
			services.AddInstance<INavigator>(this);
		}

		var executedRoute = await CoreNavigateAsync(request);


		if (navigator is not null)
		{
			return navigator.AsResponseWithResult(executedRoute);
		}


		return executedRoute;

	}

	protected virtual async Task<NavigationResponse?> CoreNavigateAsync(NavigationRequest request)
	{
		if (request.Route.IsEmpty())
		{
			var route = RouteResolver.FindByPath(this.Route?.Base);
			if (route is not null)
			{
				var defaultRoute = route.Nested?.FirstOrDefault(x => x.IsDefault);
				if (defaultRoute is not null)
				{
					request = request with { Route = request.Route.Append(defaultRoute.Path) };

				}
			}

			if (request.Route.IsEmpty())
			{
				return null;
			}
		}

		// Don't propagate the response request further than a named region
		if (!string.IsNullOrWhiteSpace(Region.Name) && request.Result is not null)
		{
			request = request with { Result = null };
		}

		// The "./" prefix is no longer required as we pass the request down the hierarchy
		request = request with { Route = request.Route.TrimScheme(Schemes.Nested) };

		var children = Region.Children.Where(region =>
										// Unnamed child regions
										string.IsNullOrWhiteSpace(region.Name) ||
										// Regions whose name matches the next route segment
										region.Name == request.Route.Base ||
										// Regions whose name matches the current route
										// eg currently selected tab
										region.Name == Route?.Base
									).ToArray();

		var tasks = new List<Task<NavigationResponse?>>();
		foreach (var region in children)
		{
			tasks.Add(region.NavigateAsync(request));
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
}
