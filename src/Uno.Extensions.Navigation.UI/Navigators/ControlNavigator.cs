using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
#if !WINUI
using Windows.System;
#else
using Microsoft.UI.Dispatching;
#endif

namespace Uno.Extensions.Navigation.Navigators;

public abstract class ControlNavigator<TControl> : ControlNavigator
    where TControl : class
{
    public virtual TControl? Control { get; }

    protected ControlNavigator(
        ILogger logger,
        IRegion region,
        IRouteResolver routeResolver, IViewResolver viewResolver,
        TControl? control)
        : base(logger, routeResolver, viewResolver, region)
    {
        Control = control;
    }

	protected override DispatcherQueue GetDispatcher() =>
#if WINUI
		(Control as FrameworkElement)?.DispatcherQueue
#else
		Windows.ApplicationModel.Core.CoreApplication.MainView.DispatcherQueue
#endif
		?? base.GetDispatcher();

	protected virtual FrameworkElement? CurrentView => default;

    protected abstract Task<string?> Show(string? path, Type? viewType, object? data);

    protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
    {
        if (Control is null)
        {
            return default;
        }

        var route = request.Route;
        var mapping = ViewResolver.FindView(route);
        Logger.LogDebugMessage($"Navigating to path '{route.Base}' with view '{mapping?.View?.Name}'");
        var executedPath = await Show(route.Base, mapping?.View, route.Data);

        InitialiseCurrentView(route, mapping);

        if (string.IsNullOrEmpty(executedPath))
        {
            return Route.Empty;
        }

        return route with { Base = executedPath, Path = null };
    }

    protected object? InitialiseCurrentView(Route route, ViewMap? mapping)
    {
        var view = CurrentView;

        if (view is null)
        {
            return null;
        }

        var navigator = Region.Navigator();
        var services = this.Get<IServiceProvider>();

        if (navigator is null ||
            services is null)
        {
            return null;
        }

        var viewModel = view.DataContext;
        if (viewModel is null ||
            viewModel.GetType() != mapping?.ViewModel)
        {
            // This will happen if cache mode isn't set to required
            viewModel = CreateViewModel(services, navigator, route, mapping);
        }

        view.InjectServicesAndSetDataContext(services, navigator, viewModel);

        return viewModel;
    }


    protected override string NavigatorToString => (Route?.ToString()) ?? string.Empty;
}

public abstract class ControlNavigator : Navigator
{
    public virtual bool CanGoBack => false;

    protected ControlNavigator(
        ILogger logger,
        IRouteResolver routeResolver, IViewResolver viewResolver,
        IRegion region)
        : base(logger, region, routeResolver, viewResolver)
    {
    }

    protected async override Task<NavigationResponse?> CoreNavigateAsync(NavigationRequest request)
    {
        var regionResponse = await RegionNavigateAsync(request);

        if (regionResponse is not null)
        {
            if (!regionResponse.Success)
            {
                return regionResponse;
            }

            request = request with { Route = request.Route.Trim(regionResponse?.Route) };
        }

        var coreResponse = await base.CoreNavigateAsync(request);

        return coreResponse ?? regionResponse;
    }

    protected virtual bool CanNavigateToRoute(Route route) => route.IsCurrent();

    private async Task<NavigationResponse?> RegionNavigateAsync(NavigationRequest request)
    {
        // If the request has come down from parent it
        // will still have the ./ prefix, so need to trim
        // it before processing it
        if (request.Route.IsNested())
        {
            request = request with { Route = request.Route.TrimScheme(Schemes.Nested) };
        }

		var completion = new TaskCompletionSource<NavigationResponse?>();
		GetDispatcher().TryEnqueue(async () =>
		{
			if (CanNavigateToRoute(request.Route))
			{
				var response =  await ControlNavigateAsync(request);
				completion.SetResult(response);
				return;
			}
			completion.SetResult(default);
		});

		return await completion.Task;
		

        //return await Task.FromResult<NavigationResponse?>(default);
    }

    public virtual void ControlInitialize()
    {
    }

	protected virtual DispatcherQueue GetDispatcher() => DispatcherQueue.GetForCurrentThread();

	protected async Task<NavigationResponse?> ControlNavigateAsync(NavigationRequest request)
    {
		var services = Region.Services;
        if (services is null)
        {
            return default;
        }

		var executedRoute = await ExecuteRequestAsync(request);

		UpdateRoute(executedRoute);

		return new NavigationResponse(executedRoute ?? Route.Empty);
	}

    protected virtual void UpdateRoute(Route? route)
    {
        Route = route is not null ? new Route(Schemes.Current, route.Base, null, route.Data) : null;
    }

    protected object? CreateViewModel(IServiceProvider services, INavigator navigator, Route route, ViewMap? mapping)
    {
        if (mapping?.ViewModel is not null)
        {
            var dataFactor = services.GetRequiredService<NavigationDataProvider>();
            dataFactor.Parameters = route.Data ?? new Dictionary<string, object>();

            var vm = services.GetService(mapping.ViewModel);
            if (vm is IInjectable<INavigator> navAware)
            {
                navAware.Inject(navigator);
            }

            if (vm is IInjectable<IServiceProvider> spAware && Region.Services is not null)
            {
                spAware.Inject(Region.Services);
            }

            return vm;
        }

        return null;
    }

    protected abstract Task<Route?> ExecuteRequestAsync(NavigationRequest request);
}
