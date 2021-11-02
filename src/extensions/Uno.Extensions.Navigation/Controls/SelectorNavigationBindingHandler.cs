#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using System;
using System.Threading.Tasks;
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

            Func<FrameworkElement, object, Task> action = async (sender, data) =>
            {
                var navdata = sender.GetData() ?? data;
                var path = sender.GetRequest();
                var nav = sender.Navigator();
                await nav.NavigateToRouteAsync(sender, path, Schemes.Current, navdata);
            };

            SelectionChangedEventHandler selectionAction = async (actionSender, actionArgs) =>
            {
                var sender = actionSender as Selector;
                if (sender is null)
                {
                    return;
                }
                var data = sender.GetData() ?? sender.SelectedItem;

                await action(sender, data);
            };

            ItemClickEventHandler clickAction = async (actionSender, actionArgs) =>
            {
                var sender = actionSender as ListViewBase;
                if (sender is null)
                {
                    return;
                }
                var data = sender.GetData() ?? actionArgs.ClickedItem;

                await action(sender, data);
            };

            Action connect = null;
            Action disconnect = null;
            if (viewList is ListViewBase lv
                    && lv.IsItemClickEnabled)
            {
                connect = () => lv.ItemClick += clickAction;
                disconnect = () => lv.ItemClick -= clickAction;
            }
            else
            {
                connect = () => viewList.SelectionChanged += selectionAction;
                disconnect = () => viewList.SelectionChanged -= selectionAction;
            }

            if (viewList.IsLoaded)
            {
                connect();
            }

            viewList.Loaded += (s, e) =>
            {
                connect();
            };
            viewList.Unloaded += (s, e) =>
            {
                disconnect();
            };
        }
    }
}
