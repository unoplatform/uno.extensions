using Uno.Extensions.Navigation.Controls;
using Uno.Toolkit.UI;
using Uno.Toolkit.UI.Controls;
#if !WINUI
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Commerce.Navigation;

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
