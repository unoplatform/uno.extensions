#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public class ButtonBaseNavigationBindingHandler : ControlNavigationBindingHandler<ButtonBase>
    {
        public override void Bind(FrameworkElement view)
        {
            var viewButton = view as ButtonBase;
            if (viewButton is null)
            {
                return;
            }

            RoutedEventHandler clickAction = async (actionSender, actionArgs) =>
            {
                var button = actionSender as ButtonBase;
                if (button is null)
                {
                    return;
                }

                var path = button.GetRequest();
                var nav = button.Navigator();
                await nav.NavigateToRouteAsync(button, path, Schemes.Current, button.GetData());
            };

            if (viewButton.IsLoaded)
            {
                viewButton.Click += clickAction;
            }

            viewButton.Loaded += (s, e) =>
            {
                var button = s as ButtonBase;
                if (button is null)
                {
                    return;
                }

                button.Click += clickAction;
            };
            viewButton.Unloaded += (s, e) =>
            {
                var button = s as ButtonBase;
                if (button is null)
                {
                    return;
                }

                button.Click -= clickAction;
            };
        }
    }
}
