namespace Uno.Extensions.Navigation.Navigators;

public abstract class SelectorNavigator<TControl> : ControlNavigator<TControl>
	where TControl : class
{
	private Action? _detachSelectionChanged;

	public override void ControlInitialize()
	{
		if (Control is not null)
		{
			_detachSelectionChanged = AttachSelectionChanged(SelectionChanged);
		}
	}

	protected abstract FrameworkElement? SelectedItem { get; set; }

	protected abstract Action? AttachSelectionChanged(Action<FrameworkElement, FrameworkElement?> selectionChanged);

	protected abstract IEnumerable<FrameworkElement> Items { get; }

	protected override FrameworkElement? CurrentView => SelectedItem;

	protected override async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		if(!await base.RegionCanNavigate(route, routeMap))
		{
			return false;
		}

		return await Dispatcher.ExecuteAsync(async () =>
		{
			return FindByPath(routeMap?.Path ?? route.Base) is not null;
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

	protected override async Task<string?> Show(string? path, Type? viewType, object? data)
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
			await SelectedItem.EnsureLoaded();

			_detachSelectionChanged = AttachSelectionChanged(SelectionChanged);
		}
	}

	private async void SelectionChanged(FrameworkElement sender, FrameworkElement? selectedItem)
	{
		if (selectedItem is null)
		{
			return;
		}

		await selectedItem.EnsureLoaded();

		var path = selectedItem.GetRegionOrElementName();
		var nav = Region.Navigator();

		if (path is null ||
			string.IsNullOrEmpty(path) ||
			nav is null)
		{
			return;
		}

		await nav.NavigateRouteAsync(sender, path);
	}


	private FrameworkElement? FindByPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || Control is null)
		{
			return default;
		}

		var item = (from mi in Items
					where (mi.GetRegionOrElementName() == path)
					select mi).FirstOrDefault();
		return item;
	}
}
