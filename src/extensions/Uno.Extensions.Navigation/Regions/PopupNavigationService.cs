using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.ViewModels;
#if !WINDOWS_UWP && !WINUI
using Popup = Windows.UI.Xaml.Controls.Popup;
#endif
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace Uno.Extensions.Navigation.Regions;

public class PopupNavigationService : ControlNavigationService<Popup>
{
    protected override object CurrentView => Control;

    protected override string CurrentPath => CurrentView?.NavigationRoute(Mappings);

    public PopupNavigationService(
        ILogger<ContentControlNavigationService> logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory,
        IScopedServiceProvider scopedServices,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, parent, serviceFactory, scopedServices, viewModelManager, mappings, controlProvider.RegionControl as Popup)
    {
    }

    protected override void Show(string path, Type view, object data)
    {
        try
        {
            Control.IsOpen = path == RouteConstants.PopupShow;
        }
        catch (Exception ex)
        {
            Logger.LazyLogError(() => $"Unable to create instance - {ex.Message}");
        }
    }
}
