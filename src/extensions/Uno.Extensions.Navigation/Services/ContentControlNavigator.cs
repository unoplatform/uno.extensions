using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.ViewModels;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Services;

public class ContentControlNavigator : ControlNavigator<ContentControl>
{
    protected override object CurrentView => Control.Content;

    protected override string CurrentPath => CurrentView?.NavigationRoute(Mappings);

    public ContentControlNavigator(
        ILogger<ContentControlNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as ContentControl)
    {
    }

    protected override void Show(string path, Type viewType, object data)
    {
        try
        {
            if (viewType is null)
            {
                Logger.LazyLogError(() => "Missing view for navigation path '{path}'");
            }

            Logger.LazyLogDebug(() => $"Creating instance of type '{viewType.Name}'");
            var content = Activator.CreateInstance(viewType);
            Control.Content = content;
            Logger.LazyLogDebug(() => "Instance created");
        }
        catch (Exception ex)
        {
            Logger.LazyLogError(() => $"Unable to create instance - {ex.Message}");
        }
    }
}
