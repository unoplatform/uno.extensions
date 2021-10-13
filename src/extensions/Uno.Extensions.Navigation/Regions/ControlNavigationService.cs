using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.ViewModels;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation.Regions;

public abstract class ControlNavigationService<TControl> : ControlNavigationService
    where TControl : class
{
    public virtual TControl Control { get; set; }

    protected IRouteMappings Mappings { get; }

    protected ControlNavigationService(
        ILogger logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory,
        IScopedServiceProvider scopedServices,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        TControl control)
        : base(logger, parent, serviceFactory, scopedServices, viewModelManager)
    {
        Mappings = mappings;
        Control = control;
    }

    public override Task RegionNavigate(NavigationContext context)
    {
        Logger.LazyLogDebug(() => $"Navigating to path '{context.Request.Route.Base}' with view '{context.Mapping?.View?.Name}'");
        Show(context.Request.Route.Base, context.Mapping?.View, context.Request.Route.Data);
        return Task.CompletedTask;
    }

    protected abstract void Show(string path, Type viewType, object data);

    public override string ToString()
    {
        return $"Region({typeof(TControl).Name}) Path='{CurrentPath}'";
    }
}

public abstract class ControlNavigationService : CompositeNavigationService
{
    protected IServiceProvider ScopedServices { get; }

    protected ILogger Logger { get; }

    protected virtual string CurrentPath => string.Empty;

    protected virtual bool CanGoBack => false;

    protected IViewModelManager ViewModelManager { get; }

    protected ControlNavigationService(
        ILogger logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory,
        IScopedServiceProvider scopedServices,
        IViewModelManager viewModelManager)
        : base(logger, parent, serviceFactory)
    {
        Logger = logger;
        ScopedServices = scopedServices;
        ViewModelManager = viewModelManager;
    }

    public virtual void ControlInitialize()
    {
    }

    protected async Task<NavigationResponse> ControlNavigateAsync(NavigationRequest request)
    {
        if (request.Route.Base == CurrentPath)
        {
            return null;
        }

        var context = request.BuildNavigationContext(ScopedServices);

        TaskCompletionSource<Options.Option> resultTask = default;
        if (request.RequiresResponse())
        {
            var responseNav = new ResponseNavigationService(context.Services.GetInstance<INavigationService>());
            resultTask = responseNav.ResultCompletion;

            context.Services.AddInstance<INavigationService>(responseNav);
        }

        await InternalNavigateAsync(context);
        return new NavigationResponse(request, resultTask?.Task);
    }

    private async Task InternalNavigateAsync(NavigationContext context)
    {
        var request = context.Request;

        if (request.Route.Base == CurrentPath)
        {
            await Task.CompletedTask;
        }

        var navigationHandled = await EndCurrentNavigationContext(context);

        if (context.CancellationToken.IsCancellationRequested)
        {
            await Task.FromCanceled(context.CancellationToken);
        }

        if (!navigationHandled)
        {
            await DoNavigation(context);

            await ViewModelManager.StartViewModel(context, CurrentViewModel);
        }

        context = context with { CanCancel = CanGoBack };

        if (context.CanCancel)
        {
            context.CancellationToken.Register(() =>
            {
                this.NavigateToPreviousViewAsync(context.Request.Sender);
            });
        }
    }

    protected virtual Task DoNavigation(NavigationContext context)
    {
        return DoForwardNavigation(context);
    }

    protected async Task DoForwardNavigation(NavigationContext context)
    {
        await RegionNavigate(context);

        InitialiseView(context);
    }

    public abstract Task RegionNavigate(NavigationContext context);

    private async Task<bool> EndCurrentNavigationContext(NavigationContext navigationContext)
    {
        if (CurrentView is not null)
        {
            // Stop the currently active viewmodel
            await ViewModelManager.StopViewModel(navigationContext, CurrentViewModel);

            if (!CanGoBack || navigationContext.IsBackNavigation)
            {
                ViewModelManager.DisposeViewModel(CurrentViewModel);
            }

            // Check if navigation was cancelled - if it is,
            // then indicate that navigation has been handled
            if (navigationContext.IsCancelled)
            {
                var completion = navigationContext.Navigation as ResponseNavigationService;
                if (completion is not null)
                {
                    completion.ResultCompletion.SetResult(Options.Option.None<object>());
                }

                return true;
            }
        }

        return false;
    }

    protected void InitialiseView(NavigationContext context)
    {
        var view = CurrentView;

        var viewModel = CurrentViewModel;
        var mapping = context.Mapping;
        if (viewModel is null || viewModel.GetType() != mapping.ViewModel)
        {
            // This will happen if cache mode isn't set to required
            viewModel = ViewModelManager.CreateViewModel(context);
        }

        view.InjectServicesAndSetDataContext(context.Services, context.Navigation, viewModel);
    }

    protected virtual object CurrentView => default;

    protected object CurrentViewModel => (CurrentView as FrameworkElement)?.DataContext;

    public override string ToString()
    {
        return $"Region Path='{CurrentPath}'";
    }


    protected async override Task<NavigationResponse> CoreNavigateAsync(NavigationRequest request)
    {
        NavigationResponse regionResponse = null;
        if (request.Route.IsCurrent)
        {
            regionResponse = await RegionNavigateAsync(request);

            if (regionResponse is not null)
            {
                request = request.Route.NextRequest(request.Sender);
            }
        }

        if (!(request?.Route?.IsNested ?? false))
        {
            return regionResponse;
        }

        return await base.CoreNavigateAsync(request);
    }

    private async Task<NavigationResponse> RegionNavigateAsync(NavigationRequest request)
    {
        var taskCompletion = new TaskCompletionSource<Options.Option>();
        // Temporarily detach all nested services to prevent accidental
        // navigation to the wrong child
        // eg switching tabs, frame on tab1 won't get detached until some
        // time after navigating to tab2, meaning that the wrong nexted
        // child will be used for any subsequent navigations.
        var children = DetachAll();
        var regionTask = await ControlNavigateAsync(request);
        if (regionTask is null)
        {
            // If a null result task was returned, then no
            // navigation took place, so just reattach the existing
            // nav services
            AttachAll(children);
        }
        else
        {
            _ = regionTask.Result?.ContinueWith((Task<Options.Option> t) =>
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

    //public override string ToString()
    //{
    //    var sb = new StringBuilder();
    //    PrintAllRegions(sb, this);
    //    return sb.ToString();
    //}

    protected override void PrintAllRegions(StringBuilder builder, IRegionNavigationService nav, int indent = 0, string regionName = null)
    {
        //    if (nav is RegionNavigationService rns)
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
    }
}
