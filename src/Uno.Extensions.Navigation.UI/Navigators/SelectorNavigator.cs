using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation.Navigators;

public abstract class SelectorNavigator<TControl> : ControlNavigator<TControl>
	where TControl : class
{
	private Action? _detachSelectionChanged;

	public override void ControlInitialize()
	{
		if (Control is not null)
		{
			_detachSelectionChanged = AttachSelectionChanged((sender, selected) => _ = SelectionChanged(sender, selected));
		}
		else
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Control is null, so unable to attach selection changed handler");
			}
		}
	}

	protected abstract FrameworkElement? SelectedItem { get; set; }

	protected abstract Action? AttachSelectionChanged(Action<FrameworkElement, FrameworkElement?> selectionChanged);

	protected abstract IEnumerable<FrameworkElement> Items { get; }

	protected override FrameworkElement? CurrentView => SelectedItem;

	protected override async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		// When a tab item exists but has no registered route (e.g. added via XAML HR),
		// lazily insert a route with a FrameView so that the sibling
		// PanelVisiblityNavigator also accepts the navigation and the request
		// propagates through the composite parent to all children.
		if (routeMap is null && !string.IsNullOrWhiteSpace(route.Base))
		{
			var hasItem = await Dispatcher.ExecuteAsync(async cancellation =>
			{
				return FindByPath(route.Base) is not null;
			});

			if (hasItem)
			{
				EnsureRouteRegistered(route.Base);
				routeMap = Resolver.FindByPath(route.Base);
			}
		}

		if (!await base.RegionCanNavigate(route, routeMap))
		{
			return false;
		}

		return await Dispatcher.ExecuteAsync(async cancellation =>
		{
			return FindByPath(routeMap?.Path ?? route.Base) is not null;
		});
	}

	/// <summary>
	/// Inserts a route for the given path if one does not already exist.
	/// The new route is created as a sibling of existing tab routes (same Parent)
	/// and inherits the View and ViewModel from a sibling so that the
	/// <see cref="PanelVisiblityNavigator"/> can create a FrameView and the
	/// inner FrameNavigator can navigate to the correct page type.
	/// </summary>
	private void EnsureRouteRegistered(string path)
	{
		if (Resolver.FindByPath(path) is not null)
		{
			return;
		}

		RouteInfo? siblingRoute = null;
		foreach (var item in Items)
		{
			var itemPath = item.GetRegionOrElementName()?.WithoutQualifier();
			if (string.IsNullOrEmpty(itemPath))
			{
				continue;
			}

			var existing = Resolver.FindByPath(itemPath);
			if (existing is not null)
			{
				siblingRoute = existing;
				break;
			}
		}

		var newRoute = new RouteInfo(
			path,
			View: siblingRoute?.View,
			ViewModel: siblingRoute?.ViewModel)
		{
			Parent = siblingRoute?.Parent
		};
		Resolver.InsertRoute(newRoute);
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

	protected override async Task<string?> Show(
		string? path,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type? viewType,
		object? data)
	{
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
			var item = FindByPath(path);

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


	private FrameworkElement? FindByPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || Control is null)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Attempting to find empty path ({path}) or Control is null ({Control is null})");
			}

			return default;
		}

		var item = (from mi in Items
					where (mi.GetRegionOrElementName().WithoutQualifier() == path)
					select mi).FirstOrDefault();
		return item;
	}
}
