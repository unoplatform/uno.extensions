using Microsoft.UI.Xaml.Media.Animation;

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

		if(routeMap?.RenderView?.IsSubclassOf(typeof(FrameworkElement)) ?? false)
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
				if (viewType is null ||
					viewType.IsSubclassOf(typeof(Page)))
				{
					viewType = typeof(UI.Controls.FrameView);
				}

				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Creating instance of type '{viewType.Name}'");
				controlToShow = Activator.CreateInstance(viewType) as FrameworkElement;
				if (controlToShow is not null)
				{
					if (!string.IsNullOrWhiteSpace(regionName) &&
						controlToShow is FrameworkElement fe)
					{
						fe.SetName(regionName!);
					}
					controlToShow.Visibility = Visibility.Visible;
					controlToShow.Opacity = 0;
					Control.Children.Add(controlToShow);
				}
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
				controlToShow.Opacity = 0;
				controlToShow.Visibility = Visibility.Visible;
			}
			CurrentlyVisibleControl = controlToShow;
		}

		Control.ReassignRegionParent();

		return path;
	}

	protected override async Task PostNavigateAsync()
	{
		if (Control is not null &&
			CurrentlyVisibleControl is not null &&
			(CurrentlyVisibleControl.Opacity<1.0 ||
			CurrentlyVisibleControl.Visibility==Visibility.Collapsed))
		{
			Control.Children.Remove(CurrentlyVisibleControl);
			Control.Children.Add(CurrentlyVisibleControl);
			CurrentlyVisibleControl.Opacity = 0;
			CurrentlyVisibleControl.Visibility = Visibility.Visible;
			var animation = new DoubleAnimation
			{
				To = 1.0,
				FillBehavior = FillBehavior.HoldEnd,
				BeginTime = TimeSpan.FromSeconds(0),
				Duration = new Duration(TimeSpan.FromSeconds(0.5))
			};
			var storyboard = new Storyboard();
			storyboard.Children.Add(animation);
			Storyboard.SetTarget(animation, CurrentlyVisibleControl);
			Storyboard.SetTargetProperty(animation, nameof(FrameworkElement.Opacity));
			var completion = new TaskCompletionSource<bool>();
			storyboard.Completed += (s, e) => completion.SetResult(false);
			storyboard.Begin();
			await completion.Task;

			foreach (var child in Control.Children.OfType<FrameworkElement>())
			{
				if(child != CurrentlyVisibleControl)
				{
					child.Opacity = 0;
					child.Visibility = Visibility.Collapsed;

				}
			}
		}
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
