using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
using System.Threading.Tasks;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Services;

public class NavigationViewNavigator : ControlNavigator<Microsoft.UI.Xaml.Controls.NavigationView>
{
    protected override FrameworkElement CurrentView => Control.SelectedItem as FrameworkElement;

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

        var path = tbi.GetName() ?? tbi.Name;
        if (!string.IsNullOrEmpty(path))
        {
            Region.Navigator().NavigateToRouteAsync(null, path);
        }
    }

    public NavigationViewNavigator(
        ILogger<NavigationViewNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as Microsoft.UI.Xaml.Controls.NavigationView)
    {
    }

    protected override async Task Show(string path, Type view, object data)
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
