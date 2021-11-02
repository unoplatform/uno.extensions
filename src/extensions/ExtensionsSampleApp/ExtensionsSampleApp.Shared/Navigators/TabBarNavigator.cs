using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.UI.ToolkitLib;

using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Navigators;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace ExtensionsSampleApp.Navigators
{
    public class TabBarRegion : ControlNavigator<TabBar>
    {
        private TabBar _control;

        public override TabBar Control
        {
            get => _control;
            set
            {
                if (_control != null)
                {
                    _control.SelectionChanged -= ControlSelectionChanged;
                }
                _control = value;
                if (_control != null)
                {
                    _control.SelectionChanged += ControlSelectionChanged;
                }
            }
        }

        private async void ControlSelectionChanged(TabBar sender, TabBarSelectionChangedEventArgs args)
        {
            var tbi = args.NewItem as TabBarItem;
            if (tbi is null)
            {
                return;
            }
            await tbi.EnsureLoaded();
            var tabName = tbi.GetName() ?? tbi.Name;
            await Region.Navigator().NavigateToRouteAsync(tbi, tabName);
        }

        public TabBarRegion(
            ILogger<TabBarRegion> logger,
            IRegion region,
            IRouteMappings mappings,
            RegionControlProvider controlProvider)
            : base(logger, region, mappings, controlProvider.RegionControl as TabBar)
        {
        }

        protected override async Task Show(string path, Type view, object data)
        {
            Control.SelectionChanged -= ControlSelectionChanged;
            if (int.TryParse(path, out var index))
            {
                Control.SelectedIndex = index;
            }
            else
            {
                if (Control.ItemsPanelRoot is null)
                {
                    return;
                }

                var item = (from tbi in Control.ItemsPanelRoot?.Children.OfType<TabBarItem>()
                            where tbi.GetName() == path || tbi.Name == path
                            select tbi).FirstOrDefault();
                if (item is not null)
                {
                    var idx = Control.IndexFromContainer(item);
                    Control.SelectedIndex = idx;
                    await (item as FrameworkElement).EnsureLoaded();
                }
            }
            Control.SelectionChanged += ControlSelectionChanged;
        }
    }
}
