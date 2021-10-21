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

namespace ExtensionsSampleApp.Region.Managers
{
    public class TabBarRegion : ControlNavigator<TabBar>
    {
        private TabBar _control;

        public override TabBar Control {
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
                Region.Navigator().NavigateByPathAsync(null, path);
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
            }
        }
    }

    //public class TabBarContentManager<TContent, TContentManager> : TabBarManager
    //    where TContent : class
    //    where TContentManager : BaseControlManager<TContent>
    //{
    //    private TContentManager ContentManager { get; }
    //    public TabBarContentManager(ILogger<PageVisualStateManager> logger, INavigationService navigation, TContentManager contentManager, RegionControlProvider controlProvider) : base(logger, navigation, controlProvider)
    //    {
    //        ContentManager = contentManager;
    //        var controls = (ValueTuple<object,object>)controlProvider.RegionControl;
    //        this.Control = controls.Item1 as TabBar;
    //        ContentManager.Control = controls.Item2 as TContent;
    //    }

    //    protected override object InternalShow(string path, Type view, object data, object viewModel)
    //    {
    //        var control = base.InternalShow(path, view, data, viewModel);
    //        ContentManager.Show(path, view, data, viewModel);

    //        return control;
    //    }
    //}


}
