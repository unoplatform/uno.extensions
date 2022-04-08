namespace Uno.Extensions.Navigation.Navigators;

public class PopupNavigator : ControlNavigator<Popup>
{
    protected override FrameworkElement? CurrentView => Control;

    public PopupNavigator(
        ILogger<ContentControlNavigator> logger,
		IWindowProvider window,
		IRegion region,
        IResolver resolver,
        RegionControlProvider controlProvider)
        : base(logger, window, region, resolver, controlProvider.RegionControl as Popup)
    {
    }

	public override void ControlInitialize()
    {
        base.ControlInitialize();

        if (Control is not null)
        {
            Control.Closed += Control_Closed;
        }
    }

    private void Control_Closed(object? sender, object e)
    {
        Region.Navigator()?.NavigateRouteAsync((sender ?? Control) ?? this, "hide");
    }

    protected override async Task<string?> Show(string? path, Type? viewType, object? data)
    {
        if (Control is null)
        {
            return string.Empty;
        }

        try
        {
            Control.IsOpen = string.Compare(path, RouteConstants.PopupShow, true) == 0;
            await (Control.Child as FrameworkElement).EnsureLoaded();
            return path;
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
        }

        return default;
    }
}
