using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;
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

    protected BaseRegion(
        ILogger logger,
        IServiceProvider scopedServices,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        TControl control) : base(logger, scopedServices, navigation, viewModelManager)
    {
        Control = control;
    }

    protected abstract void Show(string path, Type viewType, object data);
}

public abstract class BaseRegion : IRegion, IRegionNavigate
{
    private IServiceProvider ScopedServices { get; }

    protected ILogger Logger { get; }

    protected abstract NavigationContext CurrentContext { get; }

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

    public NavigationResponse NavigateAsync(NavigationRequest request)
    {
        var context = request.BuildNavigationContext(ScopedServices, new TaskCompletionSource<Options.Option>());
        var navTask = InternalNavigateAsync(context);
        return new NavigationResponse(request, navTask, context.ResultCompletion.Task);
    }

    private async Task InternalNavigateAsync(NavigationContext context)
    {
        var request = context.Request;

        if (context.Components.NavigationPath == CurrentContext?.Components.NavigationPath)
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

        InitialiseView(vm);
    }

    public abstract Task RegionNavigate(NavigationContext context);

    private async Task<bool> EndCurrentNavigationContext(NavigationContext navigationContext)
    {
        // If there's a current nav context, make sure it's stopped before
        // we proceed - this could cancel the navigation, so need to know
        // before we remove anything from backstack
        if (CurrentContext is not null)
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
                var completion = navigationContext.ResultCompletion;
                if (completion is not null)
                {
                    completion.SetResult(Options.Option.None<object>());
                }

                return true;
            }

            // If this is a back navigation then we need to pass back
            // any data to the current context. This is done by setting
            // the results on the ResultCompletion object
            // Note: We note performing the back navigation here, we just
            // passing data back to any caller that's waiting on it.
            if (navigationContext.IsBackNavigation)
            {
                var responseData = navigationContext.Components.Parameters.TryGetValue(string.Empty, out var response) ? response : default;

                var context = CurrentContext;

                var completion = context.ResultCompletion;
                if (completion is not null)
                {
                    if (context.Request.Result is not null && responseData is not null)
                    {
                        completion.SetResult(Options.Option.Some<object>(responseData));
                    }
                    else
                    {
                        completion.SetResult(Options.Option.None<object>());
                    }
                }
            }
        }

        return false;
    }

    protected void InitialiseView(object viewModel)
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

        if (view is IInjectable < INavigationService> navAware)
        {
            Logger.LazyLogDebug(() => $"Setting Navigation on IInjectable control");
            navAware.Inject(Navigation);
        }

        if (view is IInjectable<IServiceProvider> spAware)
        {
            spAware.Inject(ScopedServices);
        }
    }

    protected virtual object CurrentView => default;

    protected object CurrentViewModel => (CurrentView as FrameworkElement)?.DataContext;
}
