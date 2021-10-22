using System.Linq;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public class NavigationBinder
    {
        private FrameworkElement View { get; }

        public NavigationBinder(FrameworkElement view)
        {
            View = view;
            View.Loaded += LoadedHandler;
        }

        private void LoadedHandler(object sender, RoutedEventArgs args)
        {
            View.Loaded -= LoadedHandler;

            var binder = View.FindRegion().Services.GetServices<INavigationBindingHandler>().FirstOrDefault(x => x.CanBind(View));
            if (binder is not null)
            {
                binder.Bind(View);
            }
        }
    }
}
