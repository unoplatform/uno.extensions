using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.ViewModels;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions.Managers;


namespace ExtensionsSampleApp.Region.Managers
{
    public class NavigationViewRegionManager : SimpleRegionManager<NavigationView>
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

        public NavigationViewRegionManager(
            ILogger<NavigationViewRegionManager> logger,
            INavigationService navigation,
        IViewModelManager viewModelManager,
        IDialogFactory dialogFactory,
        RegionControlProvider controlProvider) : base(logger, navigation, viewModelManager, dialogFactory, controlProvider.RegionControl as NavigationView)
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
