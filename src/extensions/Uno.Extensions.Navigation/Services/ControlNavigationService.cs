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

public abstract class ControlNavigationService<TControl> : ControlNavigationService
    where TControl : class
{
    public virtual TControl Control { get; set; }

    protected IRouteMappings Mappings { get; }

    protected ControlNavigationService(
        ILogger logger,
        IRegion region,
        IRouteMappings mappings,
        TControl control)
        : base(logger, region)
    {
        Mappings = mappings;
        Control = control;
    }

    protected abstract void Show(string path, Type viewType, object data);

    protected override Task NavigateWithContextAsync(NavigationContext context)
    {
        Logger.LazyLogDebug(() => $"Navigating to path '{context.Request.Route.Base}' with view '{context.Mapping?.View?.Name}'");
        Show(context.Request.Route.Base, context.Mapping?.View, context.Request.Route.Data);

        InitialiseView(context);

        return Task.CompletedTask;
    }

    public override string ToString()
    {
        return $"Region({typeof(TControl).Name}) Path='{CurrentPath}'";
    }
}

public abstract class ControlNavigationService : CompositeNavigationService
{
    protected virtual string CurrentPath => string.Empty;

    protected virtual bool CanGoBack => false;

    protected virtual object CurrentView => default;

    protected object CurrentViewModel => (CurrentView as FrameworkElement)?.DataContext;

    protected ControlNavigationService(
        ILogger logger,
        IRegion region)
        : base(logger, region)
    {
    }

    protected async override Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        var regionResponse = await RegionNavigateAsync(request);

        var coreResponse = await base.CoreNavigateAsync(request);

        return coreResponse ?? regionResponse;
    }

    private async Task<NavigationResponse> RegionNavigateAsync(NavigationRequest request)
    {
        if (request.Route.IsCurrent)
        {
            var taskCompletion = new TaskCompletionSource<Options.Option>();
            // Temporarily detach all nested services to prevent accidental
            // navigation to the wrong child
            // eg switching tabs, frame on tab1 won't get detached until some
            // time after navigating to tab2, meaning that the wrong nexted
            // child will be used for any subsequent navigations.
            var children = Region?.DetachAll();
            var regionTask = await ControlNavigateAsync(request);
            if (regionTask is null)
            {
                // If a null result task was returned, then no
                // navigation took place, so just reattach the existing
                // nav services
                Region?.AttachAll(children);
            }
            else
            {
                _ = regionTask.Result?.ContinueWith((t) =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        taskCompletion.TrySetResult(t.Result);
                    }
                    else
                    {
                        taskCompletion.TrySetResult(Options.Option.None<object>());
                    }
                },
                  TaskScheduler.Current);
            }
            return new NavigationResponse(request, taskCompletion.Task);
        }

        return null;
    }

    public virtual void ControlInitialize()
    { }

    protected async Task<NavigationResponse> ControlNavigateAsync(NavigationRequest request)
    {
        if (request.Route.Base == CurrentPath)
        {
            return null;
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
            var completion = context.Navigation as ResponseNavigationService;
            if (completion is not null)
            {
                completion.ResultCompletion.SetResult(Options.Option.None<object>());
            }

            return null;
        }

        //var regionCompletion = new TaskCompletionSource<object>();
        //Region.View.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
        //{
        //    await NavigateWithContextAsync(context);
        //    regionCompletion.SetResult(null);
        //});
        //await regionCompletion.Task;

        await NavigateWithContextAsync(context);

        // Start view and viewmodels
        // (this is safe for null view or null viewmodel)
        await CurrentView.Start(request);
        await CurrentViewModel.Start(request);

        if (request.Cancellation.HasValue && CanGoBack)
        {
            request.Cancellation.Value.Register(() =>
            {
                Region.Navigation().NavigateToPreviousViewAsync(context.Request.Sender);
            });
        }

        return new NavigationResponse(request, resultTask?.Task);
    }

    protected abstract Task NavigateWithContextAsync(NavigationContext context);

    protected void InitialiseView(NavigationContext context)
    {
        var view = CurrentView;

        var viewModel = CurrentViewModel;
        var mapping = context.Mapping;
        if (viewModel is null || viewModel.GetType() != mapping.ViewModel)
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
