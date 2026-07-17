using System.Diagnostics.CodeAnalysis;

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
		if (Control is null)
		{
			return string.Empty;
		}

		// [NAV-HR-DIAG] #3130: capture the identity of what is currently shown vs. what this
		// Show pass resolves — proves whether navigation re-shows a stale (pre-HR) instance.
		if (Logger.IsEnabled(LogLevel.Warning))
		{
			Logger.LogWarningMessage($"[NAV-HR-DIAG] PanelVisiblityNavigator.Show ENTER path='{path}' viewType={viewType?.FullName ?? "<null>"} current={DescribeInstance(CurrentlyVisibleControl)} clearing {Region.Children.Count} child region(s)");
		}

		// Clear all child navigation regions
		Region.Children.Clear();

		var controlToShow = FindByPath(path);
		var reusedExistingChild = controlToShow is not null;
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

		if (Logger.IsEnabled(LogLevel.Warning))
		{
			Logger.LogWarningMessage($"[NAV-HR-DIAG] PanelVisiblityNavigator.Show RESOLVED path='{path}' -> {DescribeInstance(controlToShow)} (reused-existing-child={reusedExistingChild})");
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

	protected override Task CheckLoadedAsync()
		=> CurrentlyVisibleControl is { IsLoaded: false }
			? CurrentlyVisibleControl.EnsureLoaded()
			: Task.CompletedTask;

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

				// [NAV-HR-DIAG] #3130: after visibility pass, dump each child (incl. what the
				// FrameView's inner Frame is actually displaying) so the stale-vs-fresh instance
				// on the visible tab is provable from the logs.
				if (Logger.IsEnabled(LogLevel.Warning))
				{
					foreach (var child in Control.Children.OfType<FrameworkElement>())
					{
						var frameContent = child is UI.Controls.FrameView fv && fv.Content is Frame navFrame
							? $" frame.Content={DescribeInstance(navFrame.Content as FrameworkElement)}"
							: string.Empty;
						Logger.LogWarningMessage($"[NAV-HR-DIAG] PanelVisiblityNavigator.PostNavigate child={DescribeInstance(child)} name='{child.GetName() ?? child.Name}' vis={child.Visibility} opacity={child.Opacity}{frameContent}");
					}
				}
			});
		}
	}

	// [NAV-HR-DIAG] #3130: identity string (type + identity hash) so two instances of the
	// same page type (stale placeholder vs. HR-built) are distinguishable in the logs.
	private static string DescribeInstance(FrameworkElement? element)
		=> element is null
			? "<null>"
			: $"{element.GetType().FullName}#{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(element):X8}";

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
