﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Regions;
#if !WINUI
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Navigators;

public class PanelVisiblityNavigator : ControlNavigator<Panel>
{
    public const string NavigatorName = "Visibility";

    protected override FrameworkElement? CurrentView => CurrentlyVisibleControl;

    public PanelVisiblityNavigator(
        ILogger<PanelVisiblityNavigator> logger,
        IRegion region,
        IRouteMappings mappings,
        RegionControlProvider controlProvider)
        : base(logger, region, mappings, controlProvider.RegionControl as Grid)
    {
    }

    private FrameworkElement? CurrentlyVisibleControl { get; set; }

    protected override async Task<string?> Show(string? path, Type? viewType, object? data)
    {
        if(Control is null)
        {
            return string.Empty;
        }

        var controlToShow =
            Control.Children.OfType<FrameworkElement>().FirstOrDefault(x => x.GetName() == path) ??
            Control.FindName(path) as FrameworkElement;

        if (controlToShow is null)
        {
            try
            {
                if (viewType is null)
                {
                    Logger.LogErrorMessage("Missing view for navigation path '{path}'");
                    return default;
                }

                Logger.LogDebugMessage($"Creating instance of type '{viewType.Name}'");
                controlToShow = Activator.CreateInstance(viewType) as FrameworkElement;
                if (controlToShow is FrameworkElement fe)
                {
                    fe.SetName(path??string.Empty);
                }
                Control.Children.Add(controlToShow);
                Logger.LogDebugMessage("Instance created");
            }
            catch (Exception ex)
            {
                Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
            }
        }

		if (controlToShow != CurrentlyVisibleControl)
		{
			if (controlToShow is not null)
			{
				controlToShow.Visibility = Visibility.Visible;
			}

			if (CurrentlyVisibleControl != null)
			{
				CurrentlyVisibleControl.Visibility = Visibility.Collapsed;
			}
			CurrentlyVisibleControl = controlToShow;

			await controlToShow.EnsureLoaded();
		}

        return path;
    }
}
