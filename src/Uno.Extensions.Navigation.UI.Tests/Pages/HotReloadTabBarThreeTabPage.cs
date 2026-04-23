using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;
using Uno.Toolkit.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Three-tab host page for TabBar HR tests requiring more than two tabs.
/// Layout is identical to <see cref="HotReloadTabBarPage"/> but with a third TabBarItem.
///
///   OuterGrid (Region.Attached)
///   ├── ContentGrid (Region.Attached, Region.Navigator="Visibility")
///   └── TabBar (Region.Attached)
///       ├── TabBarItem [Region.Name="TabOne"]
///       ├── TabBarItem [Region.Name="TabTwo"]
///       └── TabBarItem [Region.Name="TabThree"]
/// </summary>
public sealed partial class HotReloadTabBarThreeTabPage : Page
{
	public Grid ContentGrid { get; }
	public TabBar TabBar { get; }

	public HotReloadTabBarThreeTabPage()
	{
		var outerGrid = new Grid();
		Region.SetAttached(outerGrid, true);
		outerGrid.RowDefinitions.Add(new RowDefinition());
		outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		ContentGrid = new Grid();
		Region.SetAttached(ContentGrid, true);
		Region.SetNavigator(ContentGrid, "Visibility");
		Grid.SetRow(ContentGrid, 0);
		outerGrid.Children.Add(ContentGrid);

		TabBar = new TabBar();
		Region.SetAttached(TabBar, true);
		Grid.SetRow(TabBar, 1);

		var tabOne = new TabBarItem { Content = "Tab One", IsSelectable = true };
		Region.SetName(tabOne, "TabOne");
		TabBar.Items.Add(tabOne);

		var tabTwo = new TabBarItem { Content = "Tab Two", IsSelectable = true };
		Region.SetName(tabTwo, "TabTwo");
		TabBar.Items.Add(tabTwo);

		var tabThree = new TabBarItem { Content = "Tab Three", IsSelectable = true };
		Region.SetName(tabThree, "TabThree");
		TabBar.Items.Add(tabThree);

		outerGrid.Children.Add(TabBar);
		Content = outerGrid;
	}
}
