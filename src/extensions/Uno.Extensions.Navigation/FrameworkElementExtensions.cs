using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
#endif

namespace Uno.Extensions.Navigation;

public static class FrameworkElementExtensions
{
    public static async Task EnsureLoaded(this FrameworkElement element)
    {
        if (element == null)
        {
            return;
        }

        var completion = new TaskCompletionSource<object>();

        // Note: We're attaching to three different events to
        // a) always detect when element is loaded (sometimes Loaded is never fired)
        // b) detect as soon as IsLoaded is true (Loading and Loaded not always in right order)

        RoutedEventHandler loaded = null;
        EventHandler<object> layoutChanged = null;
//#if WINDOWS_UWP || WINUI || NETSTANDARD
        TypedEventHandler<FrameworkElement, object> loading = null;
//#else
//        TypedEventHandler<DependencyObject, object> loading = null;
//#endif

        Action<bool> loadedAction = (overrideLoaded) =>
        {
            if (element.IsLoaded ||
                overrideLoaded ||
                (element.ActualHeight>0 && element.ActualWidth>0))
            {
                completion.SetResult(null);
                element.Loaded -= loaded;
                element.Loading -= loading;
                element.LayoutUpdated -= layoutChanged;
            }
        };

        loaded = (s, e) => loadedAction(false);
        loading = (s, e) => loadedAction(false);
        layoutChanged = (s, e) => loadedAction(true);

        element.Loaded += loaded;
        element.Loading += loading;
        element.LayoutUpdated += layoutChanged;

        if (element.IsLoaded ||
            (element.ActualHeight > 0 && element.ActualWidth > 0))
        {
            loadedAction(false);
        }

        await completion.Task;
    }

    public static void InjectServicesAndSetDataContext(
        this FrameworkElement view,
        IServiceProvider services,
        INavigator navigation,
        object viewModel)
    {
        if (view is not null)
        {
            if (viewModel is not null &&
                view.DataContext != viewModel)
            {
                view.DataContext = viewModel;
            }
        }

        if (view is IInjectable<INavigator> navAware)
        {
            navAware.Inject(navigation);
        }

        if (view is IInjectable<IServiceProvider> spAware)
        {
            spAware.Inject(services);
        }
    }
}
