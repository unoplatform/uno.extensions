using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation.Navigators;

public class ContentControlNavigator : ControlNavigator<ContentControl>
{
    protected override FrameworkElement? CurrentView => Control?.Content as FrameworkElement;

    public ContentControlNavigator(
        ILogger<ContentControlNavigator> logger,
        IRegion region,
        IRouteResolver routeResolver,
        RegionControlProvider controlProvider)
        : base(logger, region, routeResolver, controlProvider.RegionControl as ContentControl)
    {
    }

    protected override async Task<string?> Show(string? path, Type? viewType, object? data)
    {
        if (viewType is null ||
			viewType.IsSubclassOf(typeof(Page)))
        {
			viewType = typeof(UI.Controls.FrameView);
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Missing view for navigation path '{path}'");
			path = default;
		}

		if (Control is null)
        {
            return string.Empty;
        }

        try
        {

            if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Creating instance of type '{viewType.Name}'");
            var content = Activator.CreateInstance(viewType);
            Control.Content = content;
            await (Control.Content as FrameworkElement).EnsureLoaded();
            if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Instance created");
            return path;
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
        }

        return default;
    }
}
