using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Uno.Extensions.Navigation.Controls;

public abstract class BaseControlManager<TControl> : IInjectable
    where TControl : class
{
    protected TControl Control { get; private set; }

    public virtual void Inject(object control) => Control = control as TControl;

    protected static void InitialiseView(object view, NavigationContext context, object viewModel)
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
            navAware.Navigation = context.Services.GetService<INavigationService>();
        }
    }
}
