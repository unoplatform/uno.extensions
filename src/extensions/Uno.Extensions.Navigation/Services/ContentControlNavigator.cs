﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Services;

public class ContentControlNavigator : ControlNavigator<ContentControl>
{
    protected override FrameworkElement CurrentView => Control.Content as FrameworkElement;

    public ContentControlNavigator(
        ILogger<ContentControlNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as ContentControl)
    {
    }

    protected override async Task Show(string path, Type viewType, object data)
    {
        try
        {
            if (viewType is null)
            {
                Logger.LogErrorMessage("Missing view for navigation path '{path}'");
            }

            Logger.LogDebugMessage($"Creating instance of type '{viewType.Name}'");
            var content = Activator.CreateInstance(viewType);
            Control.Content = content;
            await (Control.Content as FrameworkElement).EnsureLoaded();
            Logger.LogDebugMessage("Instance created");
        }
        catch (Exception ex)
        {
            Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
        }
    }
}
