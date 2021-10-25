using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.ViewModels;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation.Services;

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

    protected abstract Task Show(string path, Type viewType, object data);

    protected override async Task NavigateWithContextAsync(NavigationContext context)
    {
        Logger.LogDebugMessage($"Navigating to path '{context.Request.Route.Base}' with view '{context.Mapping?.View?.Name}'");
        await Show(context.Request.Route.Base, context.Mapping?.View, context.Request.Route.Data);

        InitialiseView(context);
    }

    public override string ToString()
    {
        return $"Region({typeof(TControl).Name}) Path='{CurrentPath}'";
    }
}

public abstract class ControlNavigator : Navigator
{
    protected virtual string CurrentPath => string.Empty;

    protected virtual bool CanGoBack => false;

    protected virtual FrameworkElement CurrentView => default;

    protected object CurrentViewModel => CurrentView?.DataContext;

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

            var requestMap = this.Get<IServiceProvider>().GetService<IRouteMappings>().FindByPath(request.Route.Base);

            request = request with { Route = request.Route with { Scheme = Schemes.Current, Base = request.Route.NextBase(), Path = request.Route.NextPath() } };

            if (requestMap?.RegionInitialization is not null)
            {
                request = requestMap.RegionInitialization(Region, request);
            }
        }

        var coreResponse = await base.CoreNavigateAsync(request);

        return coreResponse ?? regionResponse;
    }

    private Task<NavigationResponse> RegionNavigateAsync(NavigationRequest request)
    {
        if (request.Route.IsCurrent)
        {
            return ControlNavigateAsync(request);
        }
        return Task.FromResult<NavigationResponse>(default);
    }

    public virtual void ControlInitialize()
    { }

    protected async Task<NavigationResponse> ControlNavigateAsync(NavigationRequest request)
    {
        if (request.Route.Base == CurrentPath)
        {
            return new NavigationResponse(request);
        }

        // Prepare the NavigationContext
        // - scoped IServiceProvider
        // - inner nav service (or wrapped in a response nav service)
        // - cancellation source
        // - mapping
        var resultTask = request.RequiresResponse() ? new TaskCompletionSource<Options.Option>() : default;
        var context = request.BuildNavigationContext(Region.Services, resultTask);

        // Notify current view and viewmodel that about to navigate.
        // If either return true, cancel nav by returning null
        // (this is safe for null view or null viewmodel)
        if (
            !await CurrentView.Stop(request) ||
            !await CurrentViewModel.Stop(request)
            )
        {
            var completion = context.Navigation as ResponseNavigator;
            if (completion is not null)
            {
                completion.ResultCompletion.SetResult(Options.Option.None<object>());
            }

            return new NavigationResponse(request, false);
        }

        //var regionCompletion = new TaskCompletionSource<object>();
        //Region.View.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
        //{
        //    await NavigateWithContextAsync(context);
        //    regionCompletion.SetResult(null);
        //});
        //await regionCompletion.Task;

        await NavigateWithContextAsync(context);

        if (request.Cancellation.HasValue && CanGoBack)
        {
            request.Cancellation.Value.Register(() =>
            {
                context.Cancel();
                context.Navigation.NavigateToPreviousViewAsync(context.Request.Sender);
            });
        }

        // Start view and viewmodels
        // (this is safe for null view or null viewmodel)
        await CurrentView.Start(request);
        await CurrentViewModel.Start(request);

        return new NavigationResultResponse(request, resultTask?.Task);
    }

    protected abstract Task NavigateWithContextAsync(NavigationContext context);

    protected void InitialiseView(NavigationContext context)
    {
        var view = CurrentView;

        var viewModel = CurrentViewModel;
        var mapping = context.Mapping;
        if (viewModel is null || viewModel.GetType() != mapping?.ViewModel)
        {
            // This will happen if cache mode isn't set to required
            viewModel = context.CreateViewModel();
        }

        view.InjectServicesAndSetDataContext(context.Services, context.Navigation, viewModel);
    }

    public override string ToString()
    {
        return $"Region Path='{CurrentPath}'";
    }

    //public override string ToString()
    //{
    //    var sb = new StringBuilder();
    //    PrintAllRegions(sb, this);
    //    return sb.ToString();
    //}

    //protected override void PrintAllRegions(StringBuilder builder, IRegionNavigationService nav, int indent = 0, string regionName = null)
    //{
    //    if (nav is ControlNavigationService rns)
    //    {
    //        if (rns.Region is null)
    //        {
    //            builder.AppendLine(string.Empty);
    //            builder.AppendLine("------------------------------------------------------------------------------------------------");
    //            builder.AppendLine($"ROOT");
    //        }
    //        else
    //        {
    //            var ans = nav;
    //            var prefix = string.Empty;
    //            if (indent > 0)
    //            {
    //                prefix = new string(' ', indent * 2) + "|-";
    //            }
    //            var reg = !string.IsNullOrWhiteSpace(regionName) ? $"({regionName}) " : null;
    //            builder.AppendLine($"{prefix}{reg}{rns.Region?.ToString()}");
    //        }
    //    }

    //    base.PrintAllRegions(builder, nav, indent, regionName);

    //    if (nav is RegionNavigationService rns2 &&
    //        rns2.Region is null)
    //    {
    //        builder.AppendLine("------------------------------------------------------------------------------------------------");
    //    }
    //}
}
