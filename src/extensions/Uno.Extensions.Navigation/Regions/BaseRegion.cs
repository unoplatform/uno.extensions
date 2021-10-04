using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
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
    private IServiceProvider ScopedServices { get; }

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
        var context = request.BuildNavigationContext(ScopedServices);

        TaskCompletionSource<Options.Option> resultTask = default;
        if (request.RequiresResponse())
        {
            var responseNav = new ResponseNavigationService(context.Services.GetService<ScopedServiceHost<IRegionNavigationService>>().Service);
            resultTask = responseNav.ResultCompletion;
            context.Services.GetService<ScopedServiceHost<INavigationService>>().Service = responseNav;
        }

        await InternalNavigateAsync(context);
        return new NavigationResponse(request, resultTask?.Task);
    }

    private async Task InternalNavigateAsync(NavigationContext context)
    {
        var request = context.Request;

        if (context.Components.NavigationPath == CurrentPath)
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
        var vm = ViewModelManager.CreateViewModel(context);

        await RegionNavigate(context);

        InitialiseView(context, vm);
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

    protected void InitialiseView(NavigationContext context, object viewModel)
    {
        var view = CurrentView;

        if (view is FrameworkElement fe)
        {
            if (viewModel is not null && fe.DataContext != viewModel)
            {
                Logger.LazyLogDebug(() => $"Setting DataContext with view model '{viewModel.GetType().Name}");
                fe.DataContext = viewModel;
            }
        }

        if (view is IInjectable<INavigationService> navAware)
        {
            Logger.LazyLogDebug(() => $"Setting Navigation on IInjectable control");
            navAware.Inject(context.Navigation);
        }

        if (view is IInjectable<IServiceProvider> spAware)
        {
            spAware.Inject(context.Services);
        }
    }

    protected virtual object CurrentView => default;

    protected object CurrentViewModel => (CurrentView as FrameworkElement)?.DataContext;

    public override string ToString()
    {
        return $"Region Path='{CurrentPath}'";
    }
}
