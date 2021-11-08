#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public class NavigationViewItemNavigationBindingHandler : ActionNavigationBindingHandlerBase<NavigationViewItem>
    {
        public override void Bind(FrameworkElement view)
        {
            var viewButton = view as NavigationViewItem;
            if (viewButton is null)
            {
                return;
            }

            var parent = VisualTreeHelper.GetParent(view);
            while (parent is not null && parent is not NavigationView)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent is null)
            {
                return;
            }
            BindAction<NavigationView, TypedEventHandler<NavigationView, NavigationViewItemInvokedEventArgs>>((NavigationView)parent,
                action => new TypedEventHandler<NavigationView, NavigationViewItemInvokedEventArgs>((sender, args) =>
                {
                    if ((args.InvokedItemContainer is FrameworkElement navItem && navItem == viewButton))
                    {
                        action((FrameworkElement)args.InvokedItemContainer);
                    }
                }),
                (element, handler) => element.ItemInvoked += handler,
                (element, handler) => element.ItemInvoked -= handler);
        }
    }
}
