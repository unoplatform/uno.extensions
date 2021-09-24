using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation.Controls;

public abstract class BaseControlManager<TControl> : IViewManager
    where TControl : class
{
    protected ILogger Logger { get; }

    public virtual TControl Control { get; set; }

    protected INavigationService Navigation { get; }

    protected BaseControlManager(ILogger logger, INavigationService navigation, TControl control)
    {
        Logger = logger;
        Navigation = navigation;
        Control = control;
    }

    /// <summary>
    /// Displays the view specified by either the path or the viewType (depending
    /// on the implementation/control)
    /// </summary>
    /// <param name="path">The navigation path (eg used by TabManager to switch based on tab name)</param>
    /// <param name="viewType">The type of view to navigation to (eg used by the FrameManager to navigate)</param>
    /// <param name="data">The data passed into the navigation (eg set as parameter for Navigate method in FrameManager)</param>
    /// <param name="viewModel">The view model to be set as datacontext on the destination view</param>
    public void Show(string path, Type viewType, object data, object viewModel)
    {
        var view = InternalShow(path, viewType, data, viewModel);
        InitialiseView(view, viewModel);
    }

    /// <summary>
    /// Shows the corresponding view
    /// </summary>
    /// <param name="path">The navigation path (eg used by TabManager to switch based on tab name)</param>
    /// <param name="viewType">The type of view to navigation to (eg used by the FrameManager to navigate)</param>
    /// <param name="data">The data passed into the navigation (eg set as parameter for Navigate method in FrameManager)</param>
    /// <returns>The control that's been navigated to</returns>
    protected abstract object InternalShow(string path, Type viewType, object data, object viewModel);

    /// <summary>
    /// Sets the view model as the data context for the view
    /// Also sets the Navigation property if the view implements
    /// INavigationAware
    /// </summary>
    /// <param name="view">The element to set the datacontext on</param>
    /// <param name="viewModel">The viewmodel to set as datacontext</param>
    protected void InitialiseView(object view, object viewModel)
    {
        if (view is FrameworkElement fe)
        {
            if (viewModel is not null && fe.DataContext != viewModel)
            {
                Logger.LazyLogDebug(() => $"Setting DataContext with view model '{viewModel.GetType().Name}");
                fe.DataContext = viewModel;
            }
        }

        if (view is INavigationAware navAware)
        {
            Logger.LazyLogDebug(() => $"Setting Navigation on INavigationAware control");
            navAware.Navigation = Navigation;
        }
    }
}
