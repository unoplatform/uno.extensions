using System.Linq;
using Microsoft.Extensions.DependencyInjection;
#if !WINUI
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

        private async void LoadedHandler(object sender, RoutedEventArgs args)
        {
            View.Loaded -= LoadedHandler;

            var region = View.FindRegion();

            if (region is not null) {
                await region.EnsureLoaded();

                var binder = region.Services?.GetServices<INavigationBindingHandler>().FirstOrDefault(x => x.CanBind(View));
                if (binder is not null)
                {
                    binder.Bind(View);
                }
            }
        }
    }
}
