namespace Uno.Extensions.Navigation.Navigators;

public class PanelVisiblityNavigator : ControlNavigator<Panel>
{
	public const string NavigatorName = "Visibility";

	protected override FrameworkElement? CurrentView => CurrentlyVisibleControl;

	public PanelVisiblityNavigator(
		ILogger<PanelVisiblityNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, dispatcher, region, resolver, controlProvider.RegionControl as Grid)
	{
	}

	protected override bool CanNavigateToRoute(Route route) =>
		base.CanNavigateToRoute(route) && 
		(
			(FindByPath(Resolver.Find(route)?.Path ?? route.Base) is not null) ||
			(Resolver.Find(route)?.RenderView?.IsSubclassOf(typeof(FrameworkElement)) ?? false)		
		);

	private FrameworkElement? CurrentlyVisibleControl { get; set; }

    protected override async Task<string?> Show(string? path, Type? viewType, object? data)
    {
        if(Control is null)
        {
            return string.Empty;
        }

		// Clear all child navigation regions
		Region.Children.Clear();

		var controlToShow = FindByPath(path);
        if (controlToShow is null)
        {
            try
            {
				var regionName = path;
				if (viewType is null ||
					viewType.IsSubclassOf(typeof(Page)))
				{
					viewType = typeof(UI.Controls.FrameView);
				}

				if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Creating instance of type '{viewType.Name}'");
                controlToShow = Activator.CreateInstance(viewType) as FrameworkElement;
                if (!string.IsNullOrWhiteSpace(regionName) &&
					controlToShow is FrameworkElement fe)
                {
                    fe.SetName(regionName!);
                }
                Control.Children.Add(controlToShow);
                if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Instance created");
            }
            catch (Exception ex)
            {
                if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
            }
        }

		if(controlToShow is UI.Controls.FrameView)
		{
			path = default;
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

			controlToShow?.ReassignRegionParent();
		}

        return path;
    }

	private FrameworkElement? FindByPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return default;
		}

		var controlToShow =
			Control.Children.OfType<FrameworkElement>().FirstOrDefault(x => x.GetName() == path) ??
			Control.FindName(path) as FrameworkElement;
		return controlToShow;
	}
}
