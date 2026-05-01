using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation.Navigators;

public class PanelVisiblityNavigator : ControlNavigator<Panel>
{
	public const string NavigatorName = "Visibility";

	// Tracks whether Show() has been called by the normal route cascade.
	// Used to detect when initial content was missed during XAML HR.
	private bool _showCalled;

	protected override FrameworkElement? CurrentView => CurrentlyVisibleControl;

	public PanelVisiblityNavigator(
		ILogger<PanelVisiblityNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, dispatcher, region, resolver, controlProvider.RegionControl as Grid)
	{
		if (region.View is { } view)
		{
			if (view.IsLoaded)
			{
				HandlePanelChildren();
			}
			else
			{
				region.View.Loaded += PanelLoaded;
			}
		}
	}

	public override void ControlInitialize()
	{
		_showCalled = false;
		_ = DeferredInitialRouteCheckAsync();
	}

	private async Task DeferredInitialRouteCheckAsync()
	{
		// Yield to the next dispatch cycle. On normal first load, the route cascade
		// calls Show() in the current cycle, so _showCalled is already true by now.
		// On XAML HR page replacement (same page type), no route cascade fires
		// for child regions, so _showCalled stays false and content goes blank.
		await Dispatcher.ExecuteAsync(async ct =>
		{
			if (_showCalled)
			{
				return;
			}

			// Walk up the region hierarchy to find a navigator with an active route.
			// The immediate parent (e.g., a Grid composite navigator) may have no route;
			// we need to reach the FrameNavigator that holds the page-level route.
			RouteInfo? parentRouteMap = null;
			var current = Region.Parent;
			while (current is not null)
			{
				var navRoute = current.Navigator()?.Route;
				if (navRoute?.Base is { Length: > 0 } basePath)
				{
					parentRouteMap = Resolver.FindByPath(basePath);
					if (parentRouteMap?.Nested is { Length: > 0 })
					{
						break;
					}
				}
				current = current.Parent;
			}

			var defaultRoute = parentRouteMap?.Nested?.FirstOrDefault(x => x.IsDefault);

			if (defaultRoute is not null)
			{
				if (Logger.IsEnabled(LogLevel.Debug))
				{
					Logger.LogDebugMessage($"Triggering deferred navigation to default route '{defaultRoute.Path}' (XAML HR)");
				}

				// Use an internal request so the route is handled locally
				// instead of being redirected up to a parent FrameNavigator.
				var request = new NavigationRequest(this, Route.PageRoute(defaultRoute.Path)).AsInternal();
				await NavigateAsync(request);
			}
		});
	}

	private void PanelLoaded(object sender, RoutedEventArgs e)
	{
		if (Control is null)
		{
			return;
		}
		Control.Loaded -= PanelLoaded;
		HandlePanelChildren();
	}

	private void HandlePanelChildren()
	{
		var existingRoutes = Control?.Children.OfType<FrameworkElement>().Select(x => x.GetName()).Where(x => x is { Length: > 0 });
		existingRoutes.ForEach(r => Resolver.InsertRoute(new RouteInfo(r))).ToArray();
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
			var path = routeMap?.Path ?? route.Base;
			var found = FindByPath(path) is not null;
			if (Logger.IsEnabled(LogLevel.Debug))
			{
				if (found)
					Logger.LogDebugMessage($"PanelVisibility: Existing child found for path '{path}'");
				else if (routeMap?.RenderView is not null)
					Logger.LogDebugMessage($"PanelVisibility: No existing child for '{path}', but view type '{routeMap.RenderView.Name}' will be created");
				else
					Logger.LogDebugMessage($"PanelVisibility: No existing child for '{path}' and no view type resolved — a FrameView will be created as fallback");
			}
			return found;
		});
	}

	private FrameworkElement? CurrentlyVisibleControl { get; set; }

	protected override async Task<string?> Show(
		string? path,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type? viewType,
		object? data)
	{
		_showCalled = true;

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

		// Only reassign region parents for the currently visible control,
		// not the entire panel. This prevents collapsed/inactive tab content 
		// regions from being re-added as children, which would cause
		// GetRoute() to pick the wrong (deepest) route from an inactive tab.
		if (controlToShow is not null)
		{
			controlToShow.ReassignRegionParent();
		}

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
}
