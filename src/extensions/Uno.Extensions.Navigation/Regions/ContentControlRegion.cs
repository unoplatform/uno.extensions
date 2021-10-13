using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.ViewModels;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Regions;

public class ContentControlRegion : ControlNavigationService<ContentControl>
{
    protected override object CurrentView => Control.Content;

    protected override string CurrentPath => CurrentView?.NavigationRoute(Mappings);

    public ContentControlRegion(
        ILogger<ContentControlRegion> logger,
        IRegionNavigationService parent,
        IRegionNavigationServiceFactory serviceFactory,
        IScopedServiceProvider scopedServices,
        IViewModelManager viewModelManager,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, parent, serviceFactory, scopedServices, viewModelManager, mappings, controlProvider.RegionControl as ContentControl)
    {
    }

    protected override void Show(string path, Type view, object data)
    {
        try
        {
            if (view is null)
            {
                Logger.LazyLogError(() => "Missing view for navigation path '{path}'");
            }

            Logger.LazyLogDebug(() => $"Creating instance of type '{view.Name}'");
            var content = Activator.CreateInstance(view);
            Control.Content = content;
            Logger.LazyLogDebug(() => "Instance created");
        }
        catch (Exception ex)
        {
            Logger.LazyLogError(() => $"Unable to create instance - {ex.Message}");
        }
    }
}
