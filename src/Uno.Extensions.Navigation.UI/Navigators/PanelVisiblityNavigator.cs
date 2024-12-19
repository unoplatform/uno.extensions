using System.Reflection;

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
		if (region.View is not null)
		{
			region.View.Loaded += PanelLoaded;
		}
	}

	private void PanelLoaded(object sender, RoutedEventArgs e)
	{
		if (Control is null)
		{
			return;
		}
		Control.Loaded -= PanelLoaded;

		var existingRoutes =
			Control.Children.OfType<FrameworkElement>().Select(x => x.GetName()).Where(x => x is { Length: > 0 });
		existingRoutes.ForEach(r => Resolver.InsertRoute(new RouteInfo(r))).ToArray();
	}

	protected override async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		// Check if the SelectorNavigator can navigate to the route
		// This is to prevent the PanelVisibilityNavigator from navigating to a route that is not specified
		// As a Region.Name in the Selector (TabBar/NavigationView) Items
		// Causing a FrameView to be wrongly injected creating a nested navigation

		var fullRoute = route.FullPath();

		// NavView usually will be a parent
		if (Region.Parent is { } parentNavigator &&
			IsRegionNavigatorSelector(parentNavigator, out var nav))
		{
			if (CanSelectorNavigate(nav!, fullRoute))
			{
				return true;
			}
		}

		// TabBar usually will be a sibling
		var sibling = Region.Parent?.Children.FirstOrDefault(x => x.View != Control);
		if (sibling is { } && IsRegionNavigatorSelector(sibling, out nav))
		{
			return CanSelectorNavigate(nav!, fullRoute);
		}

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

	private bool IsRegionNavigatorSelector(IRegion region, out INavigator? navigator)
	{
		navigator = region.Navigator();
		return navigator != null && InheritsFromSelector(navigator.GetType());
	}

	private bool CanSelectorNavigate(INavigator navigator, string route)
	{
		var itemsProperty = navigator.GetType().GetProperty("Items", BindingFlags.NonPublic | BindingFlags.Instance);
		if (itemsProperty?.GetValue(navigator) is IEnumerable<FrameworkElement> items)
		{
			return items.Any(x => x.GetName() == route);
		}

		return false;
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
				controlToShow = CreateControlFromType(viewType) as FrameworkElement;
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
		if (Control is not null)
		{
			await Dispatcher.ExecuteAsync(async cancellation =>
			{
				foreach (var child in Control.Children.OfType<FrameworkElement>())
				{
					if (child == CurrentlyVisibleControl)
					{
						child.Opacity = 1;
						child.Visibility = Visibility.Visible;
					}
					else
					{
						child.Opacity = 0;
						child.Visibility = Visibility.Collapsed;

					}
				}
			});
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

	private bool InheritsFromSelector(Type type)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SelectorNavigator<>))
		{
			return true;
		}

		var baseType = type.BaseType;

		while (baseType != null && baseType != typeof(object))
		{
			if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(SelectorNavigator<>))
			{
				return true;
			}
			baseType = baseType.BaseType;
		}

		return false;
	}
}
