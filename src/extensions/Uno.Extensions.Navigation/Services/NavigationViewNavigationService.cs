using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Services;

public class NavigationViewNavigationService : ControlNavigationService<Microsoft.UI.Xaml.Controls.NavigationView>
{
    protected override object CurrentView => Control.SelectedItem;

    protected override string CurrentPath => CurrentView?.NavigationRoute();

    private Microsoft.UI.Xaml.Controls.NavigationView _control;

    public override Microsoft.UI.Xaml.Controls.NavigationView Control
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

    private void ControlSelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
    {
        var tbi = args.SelectedItem as FrameworkElement;

        var path = Controls.Navigation.GetRoute(tbi) ?? tbi.Name;
        if (!string.IsNullOrEmpty(path))
        {
            ScopedServices.GetService<INavigationService>().NavigateByPathAsync(null, path);
        }
    }

    public NavigationViewNavigationService(
        ILogger<NavigationViewNavigationService> logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory,
        IScopedServiceProvider scopedServices,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, parent, serviceFactory, scopedServices, mappings, controlProvider.RegionControl as Microsoft.UI.Xaml.Controls.NavigationView)
    {
    }

    protected override void Show(string path, Type view, object data)
    {
        var item = (from mi in Control.MenuItems.OfType<FrameworkElement>()
                    where mi.Name == path
                    select mi).FirstOrDefault();
        if (item != null)
        {
            Control.SelectedItem = item;
        }
    }
}
