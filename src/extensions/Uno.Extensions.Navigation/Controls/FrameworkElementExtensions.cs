#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
#endif

namespace Uno.Extensions.Navigation.Controls;

public static class FrameworkElementExtensions
{
    public static async Task EnsureLoaded(this FrameworkElement element)
    {
        if (element == null)
        {
            return;
        }

        var completion = new TaskCompletionSource<object>();

        RoutedEventHandler loaded = null;
        EventHandler<object> layoutChanged = null;
        TypedEventHandler<FrameworkElement, object> loading = null;

        Action loadedAction = () =>
        {
            if (element.IsLoaded)
            {
                completion.SetResult(null);
                element.Loaded -= loaded;
                element.Loading -= loading;
                element.LayoutUpdated -= layoutChanged;
            }
        };

        loaded = (s, e) => loadedAction();
        loading = (s, e) => loadedAction();
        layoutChanged = (s, e) => loadedAction();

        element.Loaded += loaded;
        element.Loading += loading;
        element.LayoutUpdated += layoutChanged;


        if (element is null || element.IsLoaded)
        {
            loadedAction();
        }

        await completion.Task;
    }
}
