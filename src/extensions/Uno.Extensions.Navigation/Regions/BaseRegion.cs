using System;
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

public abstract class BaseRegion<TControl> : BaseRegion
    where TControl : class
{
    public virtual TControl Control { get; set; }

    protected IRouteMappings Mappings { get; }

    protected BaseRegion(
        ILogger logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        TControl control) : base(logger, scopedServices, navigation, viewModelManager)
    {
        Mappings = mappings;
        Control = control;
    }

    protected abstract void Show(string path, Type viewType, object data);

    public override string ToString()
    {
        return $"Region({typeof(TControl).Name}) Path='{CurrentPath}'";
    }
}

public abstract class BaseRegion : IRegion, IRegionNavigate
{
    protected IServiceProvider ScopedServices { get; }

    protected ILogger Logger { get; }

    protected virtual string CurrentPath => string.Empty;

    protected INavigationService Navigation { get; }

    protected virtual bool CanGoBack => false;

    protected IViewModelManager ViewModelManager { get; }

    protected BaseRegion(
        ILogger logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager)
    {
        Logger = logger;
        ScopedServices = scopedServices;
        Navigation = navigation;
        ViewModelManager = viewModelManager;
    }

    public async Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        if (request.Route.Base == CurrentPath)
        {
            return null;
        }

        var context = request.BuildNavigationContext(ScopedServices);

        TaskCompletionSource<Options.Option> resultTask = default;
        if (request.RequiresResponse())
        {
            var responseNav = new ResponseNavigationService(context.Services.GetService<ScopedServiceHost<INavigationService>>().Service);
            resultTask = responseNav.ResultCompletion;

            context.Services.GetService<ScopedServiceHost<INavigationService>>().Service = responseNav;
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
                Navigation.NavigateToPreviousViewAsync(context.Request.Sender);
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
}
