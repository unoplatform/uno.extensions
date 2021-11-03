using System;
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
    public virtual TControl Control { get; set; }

    protected IRouteMappings Mappings { get; }

    protected ControlNavigator(
        ILogger logger,
        IRegion region,
        IRouteMappings mappings,
        TControl control)
        : base(logger, region)
    {
        Mappings = mappings;
        Control = control;
    }

    protected virtual FrameworkElement CurrentView => default;

    protected abstract Task<string> Show(string path, Type viewType, object data);

    protected override async Task<Route> NavigateWithContextAsync(NavigationContext context)
    {
        Logger.LogDebugMessage($"Navigating to path '{context.Request.Route.Base}' with view '{context.Mapping?.View?.Name}'");
        var executedPath = await Show(context.Request.Route.Base, context.Mapping?.View, context.Request.Route.Data);

        InitialiseView(context);

        if (string.IsNullOrEmpty(executedPath))
        {
            return Route.Empty;
        }

        return context.Request.Route with { Base = executedPath, Path = null };
    }

    protected void InitialiseView(NavigationContext context)
    {
        var view = CurrentView;

        var viewModel = view?.DataContext;
        var mapping = context.Mapping;
        if (viewModel is null || viewModel.GetType() != mapping?.ViewModel)
        {
            // This will happen if cache mode isn't set to required
            viewModel = context.CreateViewModel();
        }

        view.InjectServicesAndSetDataContext(context.Services, context.Navigation, viewModel);
    }

    protected override string NavigatorToString => Route?.ToString();
}

public abstract class ControlNavigator : Navigator
{
    public virtual bool CanGoBack => false;

    protected ControlNavigator(
        ILogger logger,
        IRegion region)
        : base(logger, region)
    {
    }

    protected async override Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
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

    private async Task<NavigationResponse> RegionNavigateAsync(NavigationRequest request)
    {
        // Make sure the view has completely loaded before trying to process the nav request
        await Region.View.EnsureLoaded();

        if (CanNavigateToRoute(request.Route))
        {
            return await ControlNavigateAsync(request);
        }
        return await Task.FromResult<NavigationResponse>(default);
    }

    public virtual void ControlInitialize()
    { }

    protected async Task<NavigationResponse> ControlNavigateAsync(NavigationRequest request)
    {
        if (request.Route.Base == Route?.Base)
        {
            return new NavigationResponse(request.Route);
        }

        // Prepare the NavigationContext
        var resultTask = request.RequiresResponse() ? new TaskCompletionSource<Options.Option>() : default;
        var context = request.BuildNavigationContext(Region.Services, resultTask);

        var executedRoute = await NavigateWithContextAsync(context);

        UpdateRoute(executedRoute);

        if (request.Cancellation.HasValue && CanGoBack)
        {
            request.Cancellation.Value.Register(() =>
            {
                context.Cancel();
                context.Navigation.NavigateToPreviousViewAsync(context.Request.Sender);
            });
        }

        return new NavigationResultResponse(executedRoute, resultTask?.Task);
    }

    protected virtual void UpdateRoute(Route route)
    {
        Route = new Route(Schemes.Current, route.Base, null, route.Data);
    }

    protected abstract Task<Route> NavigateWithContextAsync(NavigationContext context);
}
