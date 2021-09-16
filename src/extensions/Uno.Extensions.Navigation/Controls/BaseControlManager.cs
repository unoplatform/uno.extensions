using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation.Controls;

public abstract class BaseControlManager<TControl>
    where TControl : class
{
    protected TControl Control { get; }

    protected INavigationService Navigation { get; }

    protected BaseControlManager(INavigationService navigation, TControl control)
    {
        Navigation = navigation;
        Control = control;
    }

    protected void InitialiseView(object view, object viewModel)
    {
        if (view is FrameworkElement fe)
        {
            if (viewModel is not null && fe.DataContext != viewModel)
            {
                fe.DataContext = viewModel;
            }
        }

        if (view is INavigationAware navAware)
        {
            navAware.Navigation = Navigation;
        }
    }
}
