using Uno.Extensions.Navigation.Controls;
using Uno.UI.ToolkitLib;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace ExtensionsSampleApp.Controls
{
    public class TabBarItemNavigationBindingHandler : ActionNavigationBindingHandlerBase<TabBarItem>
    {
        public override void Bind(FrameworkElement view)
        {
            var viewButton = view as TabBarItem;
            if (viewButton is null)
            {
                return;
            }

            BindAction(viewButton,
                action => new RoutedEventHandler((sender, args) => action((TabBarItem)sender)),
                (element, handler) => element.Click += handler,
                (element, handler) => element.Click -= handler);
        }
    }
}
