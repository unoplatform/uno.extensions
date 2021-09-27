using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.UI.ToolkitLib;

namespace ExtensionsSampleApp.Control.Managers
{
    public class TabBarManager : BaseControlManager<TabBar>
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
            var path = Uno.Extensions.Navigation.Controls.Navigation.GetPath(tbi) ?? tbi.Name;
            if (!string.IsNullOrEmpty(path))
            {
                Navigation.NavigateByPathAsync(null, path);
            }
        }

        public TabBarManager(ILogger<TabBarManager> logger, INavigationService navigation, RegionControlProvider controlProvider) : base(logger, navigation, controlProvider.RegionControl as TabBar)
        {
        }

        protected override object InternalShow(string path, Type view, object data, object viewModel)
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

            return null;
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
