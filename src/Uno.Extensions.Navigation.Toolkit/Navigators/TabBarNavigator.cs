using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.UI;
using Uno.Toolkit.UI;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Navigators;


namespace Uno.Extensions.Navigation.Toolkit.Navigators;

public class TabBarNavigator : ControlNavigator<TabBar>
{
	public override void ControlInitialize()
	{
		if (Control is not null)
		{
			Control.SelectionChanged += ControlSelectionChanged;
		}
	}

	protected override bool SchemeIsSupported(Route route) =>
		base.SchemeIsSupported(route) ||
		// "../" (change content) Add support for changing current content
		route.IsChangeContent();

	protected override bool CanNavigateToRoute(Route route) =>
		base.CanNavigateToRoute(route) &&
		(FindByPath(RouteResolver.Find(route)?.Path) is not null);

	private async void ControlSelectionChanged(TabBar sender, TabBarSelectionChangedEventArgs args)
	{
		var tbi = args.NewItem as TabBarItem;
		if (tbi is null)
		{
			return;
		}
		await tbi.EnsureLoaded();
		var tabName = tbi.GetName() ?? tbi.Name;
		var nav = Region.Navigator();
		if (nav is null)
		{
			return;
		}

		await nav.NavigateRouteAsync(tbi, tabName, scheme: Schemes.ChangeContent);
	}

	public TabBarNavigator(
		ILogger<TabBarNavigator> logger,
		IRegion region,
		IRouteResolver routeResolver,
		RegionControlProvider controlProvider)
		: base(logger, region, routeResolver, controlProvider.RegionControl as TabBar)
	{
	}

	protected override async Task<string?> Show(string? path, Type? viewType, object? data)
	{
		if (Control is null)
		{
			return null;
		}

		Control.SelectionChanged -= ControlSelectionChanged;
		try
		{
			if (int.TryParse(path, out var index))
			{
				Control.SelectedIndex = index;
				return path;
			}
			else
			{
				if (Control.ItemsPanelRoot is null)
				{
					return default;
				}

				var item = FindByPath(path);
				if (item is not null)
				{
					var idx = Control.IndexFromContainer(item);
					if (Control.SelectedIndex != idx)
					{
						Control.SelectedIndex = idx;
						await (item as FrameworkElement).EnsureLoaded();
					}
					return path;
				}

				return default;
			}
		}
		finally
		{
			Control.SelectionChanged += ControlSelectionChanged;
		}
	}

	private TabBarItem? FindByPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || Control is null)
		{
			return default;
		}

		var item = (from tbi in Control.Items.OfType<TabBarItem>()
					where tbi.GetName() == path || tbi.Name == path
					select tbi).FirstOrDefault();
		return item;
	}
}
