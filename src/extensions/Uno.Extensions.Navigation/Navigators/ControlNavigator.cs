using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation.Navigators;

public abstract class ControlNavigator<TControl> : ControlNavigator
    where TControl : class
{
    public virtual TControl? Control { get; }

    protected ControlNavigator(
        ILogger logger,
        IRegion region,
        IRouteMappings mappings,
        TControl? control)
        : base(logger, mappings, region)
    {
        Control = control;
    }

    protected virtual FrameworkElement? CurrentView => default;

    protected abstract Task<string?> Show(string? path, Type? viewType, object? data);

    protected override async Task<Route> ExecuteRequestAsync(NavigationRequest request)
    {
        var route = request.Route;
        var mapping = Mappings.Find(route);
        Logger.LogDebugMessage($"Navigating to path '{route.Base}' with view '{mapping?.View?.Name}'");
        var executedPath = await Show(route.Base, mapping?.View, route.Data);

        InitialiseCurrentView(route, mapping);

        if (string.IsNullOrEmpty(executedPath))
        {
            return Route.Empty;
        }

        return route with { Base = executedPath, Path = null };
    }

    protected object InitialiseCurrentView(Route route, RouteMap? mapping)
    {
        var view = CurrentView;

        var navigator = Region.Navigator();
        var services = this.Get<IServiceProvider>();
        var viewModel = view?.DataContext;
        if (viewModel is null ||
            viewModel.GetType() != mapping?.ViewModel)
        {
            // This will happen if cache mode isn't set to required
            viewModel = CreateViewModel(services, navigator, route, mapping);
        }

        view.InjectServicesAndSetDataContext(services, navigator, viewModel);

        return viewModel;
    }


    protected override string NavigatorToString => Route?.ToString();
}

public abstract class ControlNavigator : Navigator
{
    public virtual bool CanGoBack => false;

    protected IRouteMappings Mappings { get; }

    protected ControlNavigator(
        ILogger logger,
        IRouteMappings mappings,
        IRegion region)
        : base(logger, region)
    {
        Mappings = mappings;
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

            request = request with { Route = request.Route.Trim(regionResponse.Route) };
        }

        var coreResponse = await base.CoreNavigateAsync(request);

        return coreResponse ?? regionResponse;
    }

    protected virtual bool CanNavigateToRoute(Route route) => route.IsCurrent();

    private async Task<NavigationResponse?> RegionNavigateAsync(NavigationRequest request)
    {
        // Make sure the view has completely loaded before trying to process the nav request
        // Typically this might happen with the first navigation of the application where the
        // window hasn't been activated yet, so the root region may not have loaded
        await Region.View.EnsureLoaded();

        if (request.Route.IsNested() &&
            Region.Children.Any(child => child.Name == request.Route.Base))
        {
            // If the base is the name of a child, just pass the request on
            return await Task.FromResult<NavigationResponse?>(default);
        }

        // If the request has come down from parent it
        // will still have the ./ prefix, so need to trim
        // it before processing it
        if (request.Route.IsNested())
        {
            request = request with { Route = request.Route.TrimScheme(Schemes.Nested) };
        }

        if (CanNavigateToRoute(request.Route))
        {
            return await ControlNavigateAsync(request);
        }

        return await Task.FromResult<NavigationResponse?>(default);
    }

    public virtual void ControlInitialize()
    {
    }

    protected async Task<NavigationResponse> ControlNavigateAsync(NavigationRequest request)
    {
        if (request.Route.Base == Route?.Base)
        {
            return new NavigationResponse(request.Route);
        }

        var services = Region.Services;

        // Setup the navigation data (eg parameters to be injected into viewmodel)
        var dataFactor = services.GetRequiredService<NavigationDataProvider>();
        dataFactor.Parameters = request.Route.Data;

        // Create ResponseNavigator if result is requested
        TaskCompletionSource<Options.Option>? resultTask = null;
        INavigator navigator = this;
        if (request.Result is not null)
        {
            resultTask = new TaskCompletionSource<Options.Option>();
            navigator = new ResponseNavigator(navigator, request.Result, resultTask);
        }

        var executedRoute = await ExecuteRequestAsync(request);

        UpdateRoute(executedRoute);

        if (request.Cancellation.HasValue && CanGoBack)
        {
            request.Cancellation.Value.Register(() =>
            {
                navigator.NavigateToPreviousViewAsync(request.Sender);
            });
        }

        if (resultTask is not null)
        {
            return new NavigationResultResponse(executedRoute, resultTask.Task);
        }
        else
        {
            return new NavigationResponse(executedRoute);
        }
    }

    protected virtual void UpdateRoute(Route route)
    {
        Route = new Route(Schemes.Current, route.Base, null, route.Data);
    }

    protected object? CreateViewModel(IServiceProvider services, INavigator navigator, Route route, RouteMap? mapping)
    {
        if (mapping?.ViewModel is not null)
        {
            var dataFactor = services.GetRequiredService<NavigationDataProvider>();
            dataFactor.Parameters = route.Data;

            var vm = services.GetService(mapping.ViewModel);
            if (vm is IInjectable<INavigator> navAware)
            {
                navAware.Inject(navigator);
            }

            if (vm is IInjectable<IServiceProvider> spAware)
            {
                spAware.Inject(Region.Services);
            }

            return vm;
        }

        return null;
    }

    protected abstract Task<Route> ExecuteRequestAsync(NavigationRequest request);
}
