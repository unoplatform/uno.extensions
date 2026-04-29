using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation.Navigators;

public abstract class SelectorNavigator<TControl> : ControlNavigator<TControl>
	where TControl : class
{
	private Action? _detachSelectionChanged;

	// Tracks whether Show() has been called by the normal route cascade.
	// Used to detect when the initial selection was missed during XAML HR.
	private bool _showCalled;

	// Stores the nested path from the current ExecuteRequestAsync call so that
	// Show() → FindByPath() can use it to match composite region names like "Home/Favorites".
	// AsyncLocal is used to ensure correct behavior across async execution contexts.
	private static readonly AsyncLocal<string?> _currentNestedPath = new AsyncLocal<string?>();

	public override void ControlInitialize()
	{
		_showCalled = false;
		if (Control is not null)
		{
			_detachSelectionChanged = AttachSelectionChanged((sender, selected) => _ = SelectionChanged(sender, selected));

			// Schedule a deferred check for missed initial selection. During XAML HR,
			// the selector fires SelectionChanged before this navigator is created,
			// so the event is lost and content stays blank. On normal first load,
			// Show() is called by the route cascade in the same dispatch cycle,
			// setting _showCalled=true before this deferred check runs (no-op).
			_ = DeferredInitialSelectionCheckAsync();
		}
		else
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Control is null, so unable to attach selection changed handler");
			}
		}
	}

	private async Task DeferredInitialSelectionCheckAsync()
	{
		// Yield to the next dispatch cycle. On normal first load, the route cascade
		// calls Show() in the current cycle, so _showCalled is already true by now.
		// On XAML HR, no route cascade occurs, so _showCalled stays false.
		await Dispatcher.ExecuteAsync(async ct =>
		{
			if (!_showCalled &&
				SelectedItem is { } selected &&
				Region.View is FrameworkElement view)
			{
				if (Logger.IsEnabled(LogLevel.Debug))
				{
					Logger.LogDebugMessage($"Triggering navigation for missed initial selection (XAML HR)");
				}

				await SelectionChanged(view, selected);
			}
		});
	}

	protected abstract FrameworkElement? SelectedItem { get; set; }

	protected abstract Action? AttachSelectionChanged(Action<FrameworkElement, FrameworkElement?> selectionChanged);

	protected abstract IEnumerable<FrameworkElement> Items { get; }

	protected override FrameworkElement? CurrentView => SelectedItem;

	protected override async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		if (!await base.RegionCanNavigate(route, routeMap))
		{
			return false;
		}

		return await Dispatcher.ExecuteAsync(async cancellation =>
		{
			return FindByPath(routeMap?.Path ?? route.Base, route.Path) is not null;
		});
	}

	protected SelectorNavigator(
		ILogger logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, dispatcher, region, resolver, controlProvider.RegionControl as TControl)
	{
	}

	protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		_currentNestedPath.Value = request.Route.Path;
		try
		{
			return await base.ExecuteRequestAsync(request);
		}
		finally
		{
			_currentNestedPath.Value = null;
		}
	}

	protected override async Task<string?> Show(
		string? path,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type? viewType,
		object? data)
	{
		_showCalled = true;

		if (Control is null)
		{
			return null;
		}

		// Invoke detach and clean up reference to the delegate
		var detach = _detachSelectionChanged;
		_detachSelectionChanged = null;
		detach?.Invoke();
		try
		{
			var item = FindByPath(path, _currentNestedPath.Value);

			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Item to select found ({item is not null})");
			}

			// Only set the selected item if it's changed (and not null)
			// to prevent any visual artefacts that may result from setting
			// the same item multiple times
			if (item != null &&
				SelectedItem != item)
			{
				SelectedItem = item;
			}

			// Don't return path, as we need for path to be passed down to children
			return default;
		}
		finally
		{
			_detachSelectionChanged = AttachSelectionChanged((sender, selected) => _ = SelectionChanged(sender, selected));
		}
	}

	protected async Task SelectionChanged(FrameworkElement sender, FrameworkElement? selectedItem)
	{
		if (selectedItem is null)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Selected Item is null");
			}

			return;
		}

		var path = selectedItem.GetRegionOrElementName();

		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"Selected region name is {path}");
		}


		var nav = Region.Navigator();

		if (path is null ||
			string.IsNullOrEmpty(path) ||
			nav is null)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Path is {path} and Navigator is {(nav is null ? "null" : "not null")}");
			}

			return;
		}

		var data = selectedItem.GetData();

		await nav.NavigateRouteAsync(sender, path, data: data);
	}


	private FrameworkElement? FindByPath(string? path, string? nestedPath = null)
	{
		if (string.IsNullOrWhiteSpace(path) || Control is null)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Attempting to find empty path ({path}) or Control is null ({Control is null})");
			}

			return default;
		}

		// If a nested path is provided, first try to find an item whose composite region name
		// (e.g. "Home/Favorites") matches the combination of the base path and nested path.
		// This allows NavigationViewItems and TabBarItems to target sub-routes of the same region.
		if (!string.IsNullOrWhiteSpace(nestedPath))
		{
			var nestedPart = nestedPath.TrimStart('/');
			if (!string.IsNullOrWhiteSpace(nestedPart))
			{
				var compositePath = $"{path}/{nestedPart}";
				var compositeItem = (from mi in Items
									 where mi.GetRegionOrElementName().WithoutQualifier() == compositePath
									 select mi).FirstOrDefault();
				if (compositeItem is not null)
				{
					return compositeItem;
				}
			}
		}

		var item = (from mi in Items
					where (mi.GetRegionOrElementName().WithoutQualifier() == path)
					select mi).FirstOrDefault();
		return item;
	}
}
