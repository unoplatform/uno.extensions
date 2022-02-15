
namespace Uno.Extensions.Navigation.Navigators;

public abstract class SelectorNavigator<TControl> : ControlNavigator<TControl>
where TControl : class
{
	protected abstract FrameworkElement SelectedItem { get; set; }

	protected abstract Action? AttachSelectionChanged(Action<FrameworkElement, FrameworkElement?> selectionChanged);

	protected abstract IEnumerable<FrameworkElement>? Items { get; }

	protected override FrameworkElement? CurrentView => SelectedItem;

	private Action? DetachSelectionChanged { get; set; }
	public override void ControlInitialize()
	{
		if (Control is not null)
		{
			DetachSelectionChanged = AttachSelectionChanged(SelectionChanged);
		}
	}

	protected override bool SchemeIsSupported(Route route) =>
		base.SchemeIsSupported(route) ||
		// "../" (change content) Add support for changing current content
		route.IsChangeContent();

	protected override bool CanNavigateToRoute(Route route) =>
		base.CanNavigateToRoute(route) &&
		(FindByPath(RouteResolver.Find(route)?.Path) is not null);

	//private void ControlSelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
	//{
	//	var tbi = args.SelectedItem as FrameworkElement;

	//	var path = tbi?.GetName() ?? tbi?.Name;
	//	if (path is not null &&
	//		!string.IsNullOrEmpty(path))
	//	{
	//		Region.Navigator()?.NavigateRouteAsync(sender, path, scheme: Schemes.ChangeContent);
	//	}
	//}

	private async void SelectionChanged(FrameworkElement sender, FrameworkElement? selectedItem)
	{
		await selectedItem.EnsureLoaded();

		var path = selectedItem?.GetName() ?? selectedItem?.Name;
		if (path is null ||
			string.IsNullOrEmpty(path))
		{
			return;
		}

			var nav = Region.Navigator();
		if (nav is null)
		{
			return;
		}

		await nav.NavigateRouteAsync(sender, path, scheme: Schemes.ChangeContent);
	}


	protected SelectorNavigator(
		ILogger logger,
		IRegion region,
		IRouteResolver routeResolver,
		RegionControlProvider controlProvider)
		: base(logger, region, routeResolver, controlProvider.RegionControl as TControl)
	{
	}

	protected override async Task<string?> Show(string? path, Type? viewType, object? data)
	{
		if (Control is null)
		{
			return null;
		}

		DetachSelectionChanged?.Invoke();
		try
		{
			var item = FindByPath(path);
			if (item != null)
			{
				SelectedItem = item;
			}

			// Don't return path, as we need for path to be passed down to children
			return default;
		}
		finally
		{
			await SelectedItem.EnsureLoaded();
			DetachSelectionChanged = AttachSelectionChanged(SelectionChanged);
		}
	}

	private FrameworkElement? FindByPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || Control is null)
		{
			return default;
		}

		var items = Items;

		if (items == null)
		{
			return null;
		}

		var item = (from mi in Items
					where (mi.GetName() ?? mi.Name) == path
					select mi).FirstOrDefault();
		return item;
	}
}
