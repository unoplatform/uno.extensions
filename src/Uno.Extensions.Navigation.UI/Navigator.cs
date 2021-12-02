using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation.Regions;
using static Uno.Extensions.GenericExtensions;

namespace Uno.Extensions.Navigation;

public class Navigator : INavigator, IInstance<IServiceProvider>
{
	protected ILogger Logger { get; }

	protected IRegion Region { get; }

	private IRouteUpdater? RouteUpdater => Region.Services?.GetRequiredService<IRouteUpdater>();

	IServiceProvider? IInstance<IServiceProvider>.Instance => Region.Services;

	public Route? Route { get; protected set; }

	protected IMappings Mappings { get; }

	public Navigator(ILogger<Navigator> logger, IRegion region, IMappings mappings)
		: this((ILogger)logger, region, mappings)
	{
		if (region.Parent is null &&
			region.View is not null)
		{
			InitializeRootNavigator();
		}
	}

	protected Navigator(ILogger logger, IRegion region, IMappings mappings)
	{
		Region = region;
		Logger = logger;
		Mappings = mappings;
	}

	private async Task InitializeRootNavigator()
	{
		var startup = Region.Services.GetService<IStartupService>();

		// Make sure startup has completed
		await (startup?.StartupComplete() ?? Task.CompletedTask);

		var vm = CreateDefaultViewModel();
		Region.View.DataContext = vm;
	}

	public async Task<NavigationResponse?> NavigateAsync(NavigationRequest request)
	{
		Logger.LogInformation($"Pre-navigation: - {Region.ToString()}");
		try
		{
			RouteUpdater?.StartNavigation();

			// Initialise the region
			var requestMap = Region.Services?.GetRequiredService<IMappings>().FindByPath(request.Route.Base);
			if (requestMap?.Init is not null)
			{
				var newRequest = requestMap.Init(request);
				while (!request.SameRouteBase(newRequest))
				{
					request = newRequest;
					requestMap = Region.Services?.GetRequiredService<IMappings>().FindByPath(request.Route.Base);
					if (requestMap?.Init is not null)
					{
						newRequest = requestMap.Init(request);
					}
				}
				request = newRequest;
			}

			// Handle root navigations
			if (request.Route.IsRoot())
			{
				// Either
				// - forward to parent (if parent is not null)
				// - trim the Root scheme ready for handling
				if (Region.Parent is not null)
				{
					return await (Region.Parent?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse?>(default));
				}
				else
				{
					// This is the root nav service - need to pass the
					// request down to children by making the request nested
					request = request with { Route = request.Route.TrimScheme(Schemes.Root) };
				}
			}

			// Request for parent (ignore the first layer of parent scheme)
			if (request.Route.IsParent())
			{
				request = request with { Route = request.Route.TrimScheme(Schemes.Parent) };

				// Handle parent navigations
				if (request.Route.IsParent())
				{
					return await (Region.Parent?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse?>(default));
				}
			}

			// Is this region is an unnamed child of a composite,
			// send request to parent if the route has no scheme
			if ((request.Route.IsCurrent() || request.Route.IsBackOrCloseNavigation()) &&
				!Region.IsNamed() &&
				Region.Parent is not null
				&& !(Region.Children.Any(x => x.Name == request.Route.Base))
				)
			{
				return await Region.Parent.NavigateAsync(request);
			}


			// Run dialog requests
			if (request.Route.IsDialog())
			{
				request = request with { Route = request.Route with { Scheme = Schemes.Current } };
				return await DialogNavigateAsync(request);
			}

			// If the base matches the region name, than need to strip the base
			if (!string.IsNullOrWhiteSpace(request.Route.Base) &&
				request.Route.Base == Region.Name)
			{
				request = request with { Route = request.Route.Next() };
			}

			// Make sure the view has completely loaded before trying to process the nav request
			// Typically this might happen with the first navigation of the application where the
			// window hasn't been activated yet, so the root region may not have loaded
			await Region.View.EnsureLoaded();

			return await ResponseNavigateAsync(request);
		}
		finally
		{
			Logger.LogInformation($"Post-navigation: {Region.ToString()}");
			Logger.LogInformation($"Post-navigation (route): {Region.Root().GetRoute()}");
			RouteUpdater?.EndNavigation();
		}
	}

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

		var mapping = Mappings.FindView(request.Route);
		if (mapping?.UntypedBuildQuery is not null)
		{
			request = request with { Route = request.Route with { Data = request.Route.Data?.AsParameters(mapping) } };
		}

		// Setup the navigation data (eg parameters to be injected into viewmodel)
		var dataFactor = services.GetRequiredService<NavigationDataProvider>();
		dataFactor.Parameters = (request.Route?.Data) ?? new Dictionary<string, object>();

		var responseFactory = services.GetRequiredService<IResponseNavigatorFactory>();
		// Create ResponseNavigator if result is requested
		var navigator = request.GetResponseNavigator(responseFactory, this);

		var executedRoute = await CoreNavigateAsync(request);


		if (navigator is not null)
		{
			return navigator.AsResponseWithResult(executedRoute);
		}

		return executedRoute;

	}

	protected virtual async Task<NavigationResponse?> CoreNavigateAsync(NavigationRequest request)
	{
		//if (request.Route.IsNested())
		//{
		//    // At this point the request should be passed to nested, so remove
		//    // any nested scheme (ie ./ )
		//    request = request with { Route = request.Route.TrimScheme(Schemes.Nested) };// with { Scheme = Schemes.Current } };
		//}

		if (request.Route.IsCurrent() || request.Route.IsBackOrCloseNavigation())
		{
			request = request with { Route = request.Route.AppendScheme(Schemes.Nested) };
		}

		if (request.Route.IsEmpty())
		{
			return null;
		}

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

	private object? CreateDefaultViewModel()
	{
		if (Region.View is null)
		{
			return null;
		}

		var services = Region.Services;

		// Make sure the navigator is in the services so it can be used
		// when creating the view model
		services?.AddInstance<INavigator>(this);

		var mapping = Mappings.FindViewByView(Region.View.GetType());
		if (mapping?.ViewModelType is not null)
		{
			var vm = services?.GetService(mapping.ViewModelType);
			if (vm is IInjectable<INavigator> navAware)
			{
				navAware.Inject(this);
			}

			if (vm is IInjectable<IServiceProvider> spAware && Region.Services is not null)
			{
				spAware.Inject(Region.Services);
			}

			return vm;
		}

		return null;
	}
}
