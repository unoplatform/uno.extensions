using Microsoft.Extensions.DependencyInjection;

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

	protected override async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		if (!await base.RegionCanNavigate(route, routeMap))
		{
			return false;
		}

		if (routeMap?.RenderView?.IsSubclassOf(typeof(FrameworkElement)) ?? false)
		{
			return true;
		}

		return await Dispatcher.ExecuteAsync(async cancellation =>
		{
			return FindByPath(routeMap?.Path ?? route.Base) is not null;
		});
	}

	private FrameworkElement? CurrentlyVisibleControl { get; set; }

	protected override async Task<string?> Show(string? path, Type? viewType, object? data)
	{
		if (Control is null)
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
				var originalViewType = viewType;
				if (viewType is null ||
					viewType.IsSubclassOf(typeof(Page)))
				{
					viewType = typeof(BaseFrameView);
				}

				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Creating instance of type '{viewType.Name}'");
				controlToShow = viewType == typeof(BaseFrameView) ?
							Region.Services?.GetRequiredService<BaseFrameView>() :
							Activator.CreateInstance(viewType) as FrameworkElement;
				if (!string.IsNullOrWhiteSpace(regionName) &&
					controlToShow is not null)
				{
					controlToShow.SetName(regionName!);
				}
				Control.Children.Add(controlToShow);
				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Instance created");
			}
			catch (Exception ex)
			{
				if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
			}
		}

		if (controlToShow is UI.Controls.FrameView)
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
		}

		Control.ReassignRegionParent();

		return path;
	}

	private FrameworkElement? FindByPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || Control is null)
		{
			return default;
		}

		var controlToShow =
			Control.Children.OfType<FrameworkElement>().FirstOrDefault(x => x.GetName() == path) ??
			Control.FindName(path) as FrameworkElement;
		return controlToShow;
	}
}
