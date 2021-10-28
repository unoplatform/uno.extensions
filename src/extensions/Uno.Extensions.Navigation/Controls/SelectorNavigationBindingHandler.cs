#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
#endif

namespace Uno.Extensions.Navigation.Controls
{
    public class SelectorNavigationBindingHandler : ControlNavigationBindingHandler<Selector>
    {
        public override void Bind(FrameworkElement view)
        {
            var viewList = view as Selector;
            if (viewList is null)
            {
                return;
            }

            SelectionChangedEventHandler action = async (actionSender, actionArgs) =>
            {
                var list = actionSender as Selector;
                if (list is null)
                {
                    return;
                }

                var path = list.GetRequest();
                var nav = list.Navigator();
                var data = list.GetData() ?? list.SelectedItem;
                await nav.NavigateToRouteAsync(list, path, Schemes.Current, list.GetData());
            };

            if (viewList.IsLoaded)
            {
                viewList.SelectionChanged += action;
            }

            viewList.Loaded += (s, e) =>
            {
                var button = s as Selector;
                if (button is null)
                {
                    return;
                }

                button.SelectionChanged += action;
            };
            viewList.Unloaded += (s, e) =>
            {
                var button = s as Selector;
                if (button is null)
                {
                    return;
                }

                button.SelectionChanged -= action;
            };
        }
    }
}
