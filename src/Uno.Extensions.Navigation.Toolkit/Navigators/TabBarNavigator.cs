using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.UI;
using Uno.Toolkit.UI;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.Navigators;


namespace Uno.Extensions.Navigation.Toolkit.Navigators;

public class TabBarNavigator : ControlNavigator<TabBar>
{
	protected override bool RequiresDefaultView => true;

	public override void ControlInitialize()
	{
		if (Control is not null)
		{
			Control.SelectionChanged += ControlSelectionChanged;
		}
	}

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

		await nav.NavigateRouteAsync(tbi, tabName);
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

				var item = (from tbi in Control.ItemsPanelRoot?.Children.OfType<TabBarItem>()
							where tbi.GetName() == path || tbi.Name == path
							select tbi).FirstOrDefault();
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
}
