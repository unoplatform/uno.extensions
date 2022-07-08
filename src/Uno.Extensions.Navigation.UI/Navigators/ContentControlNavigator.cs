namespace Uno.Extensions.Navigation.Navigators;

public class ContentControlNavigator : ControlNavigator<ContentControl>
{
	protected override FrameworkElement? CurrentView => Control?.Content as FrameworkElement;

	public ContentControlNavigator(
		ILogger<ContentControlNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, dispatcher, region, resolver, controlProvider.RegionControl as ContentControl)
	{
	}

	protected override bool CanNavigateToDependentRoutes => true;

	protected override async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		if (!await base.RegionCanNavigate(route, routeMap))
		{
			return false;
		}

		if(routeMap is null )
		{
			return false;
		}

		var view = routeMap.RenderView;

		return view?.IsSubclassOf(typeof(FrameworkElement)) ??
			(routeMap.View is null && routeMap.ViewModel is not null); // Inject a FrameView if no View specified but there is a viewmodel (eg for shellviewmodel scenario) (ie return true if view is null)
	}
	protected override async Task<string?> Show(string? path, Type? viewType, object? data)
	{
		if (viewType is null ||
			viewType.IsSubclassOf(typeof(Page)))
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Missing view for navigation path '{path}'");
			path = viewType is null ? path : default;
			viewType = typeof(UI.Controls.FrameView);
		}

		if (Control is null)
		{
			return string.Empty;
		}

		try
		{
			// Clear all child navigation regions
			Region.Children.Clear();

			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Creating instance of type '{viewType.Name}'");
			var content = Activator.CreateInstance(viewType);
			if (path is not null &&
					content is UI.Controls.FrameView fe)
			{
				fe.SetName(path);
			}
			Control.Content = content;


			if (await (Control.Content as FrameworkElement).EnsureLoaded())
			{
				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Instance created");
				return path;
			}
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
		}

		// Content not loaded, so dispose of content and remove all children
		Control.Content = null;
		Region.Children.Clear();

		return default;
	}
}
