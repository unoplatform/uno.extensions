using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls.Managers;

public class ContentControlManager : BaseControlManager<ContentControl>
{
    public ContentControlManager(ILogger<ContentControlManager> logger, INavigationService navigation, RegionControlProvider controlProvider) : base(logger, navigation, controlProvider.RegionControl as ContentControl)
    {
    }

    protected override object InternalShow(string path, Type view, object data, object viewModel)
    {
        try
        {
            if(view is null)
            {
                Logger.LazyLogError(() => "Missing view for navigation path '{path}'");
                return null;
            }

            Logger.LazyLogDebug(() => $"Creating instance of type '{view.Name}'");
            var content = Activator.CreateInstance(view);
            Control.Content = content;
            Logger.LazyLogDebug(() => "Instance created");

            return Control.Content;
        }
        catch (Exception ex)
        {
            Logger.LazyLogError(() => $"Unable to create instance - {ex.Message}");
            return null;
        }
    }
}
