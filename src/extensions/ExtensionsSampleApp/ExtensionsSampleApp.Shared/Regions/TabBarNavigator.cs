using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.UI.ToolkitLib;
using Uno.Extensions.Navigation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Services;
using Uno.Extensions.Navigation.Regions;
using System.Threading.Tasks;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace ExtensionsSampleApp.Region.Managers
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

        private void ControlSelectionChanged(TabBar sender, TabBarSelectionChangedEventArgs args)
        {
            var tbi = args.NewItem as TabBarItem;
            var path = tbi.GetName() ?? tbi.Name;
            if (!string.IsNullOrEmpty(path))
            {
                var request = Mappings.FindByPath(path).AsRequest(this);
                var context = request.BuildNavigationContext(Region.Services);

                InitialiseView(context);
            }
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
                var item = (from tbi in Control.ItemsPanelRoot?.Children.OfType<TabBarItem>()
                            where tbi.Name == path
                            select tbi).FirstOrDefault();
                var idx = Control.IndexFromContainer(item);
                Control.SelectedIndex = idx;
                await (item as FrameworkElement).EnsureLoaded();
            }
            Control.SelectionChanged += ControlSelectionChanged;
        }
    }
}
