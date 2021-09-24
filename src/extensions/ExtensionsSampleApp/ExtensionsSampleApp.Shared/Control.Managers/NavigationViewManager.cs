using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace ExtensionsSampleApp.Control.Managers
{
    public class NavigationViewManager : BaseControlManager<NavigationView>
    {
        private NavigationView _control;

        public override NavigationView Control
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

        private void ControlSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var tbi = args.SelectedItem as FrameworkElement;

            var path = Uno.Extensions.Navigation.Controls.Navigation.GetPath(tbi) ?? tbi.Name;
            if (!string.IsNullOrEmpty(path))
            {
                Navigation.NavigateByPathAsync(null, path);
            }
        }

        public NavigationViewManager(ILogger<NavigationViewManager> logger, INavigationService navigation, RegionControlProvider controlProvider) : base(logger, navigation, controlProvider.RegionControl as NavigationView)
        {
        }

        protected override object InternalShow(string path, Type view, object data, object viewModel)
        {
            var item = (from mi in Control.MenuItems.OfType<FrameworkElement>()
                        where mi.Name == path
                        select mi).FirstOrDefault();
            if (item != null)
            {
                Control.SelectedItem = item;
            }

            return Control.SelectedItem;
        }
    }
}
