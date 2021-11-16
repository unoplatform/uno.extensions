using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.Toolkit.UI.Controls;

using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Regions;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Navigators;

#if !WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Toolkit.Navigators;

public class TabBarNavigator: ControlNavigator<TabBar>
{
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
		if(nav is null)
		{
			return;
		}

		await nav.NavigateToRouteAsync(tbi, tabName);
	}

	public TabBarNavigator(
		ILogger<TabBarNavigator> logger,
		IRegion region,
		IRouteMappings mappings,
		RegionControlProvider controlProvider)
		: base(logger, region, mappings, controlProvider.RegionControl as TabBar)
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
					Control.SelectedIndex = idx;
					await (item as FrameworkElement).EnsureLoaded();
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
