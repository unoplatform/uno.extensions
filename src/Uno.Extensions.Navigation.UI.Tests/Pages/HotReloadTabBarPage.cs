using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;
using Uno.Toolkit.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Host page for TabBar-based HR tests. Layout:
///
///   OuterGrid (Region.Attached)
///   ├── ContentGrid (Region.Attached, Region.Navigator="Visibility")
///   └── TabBar (Region.Attached)
///       ├── TabBarItem [Region.Name="TabOne"]
///       └── TabBarItem [Region.Name="TabTwo"]
///
/// The <see cref="TabBarNavigator"/> (registered via <c>UseToolkitNavigation</c>) handles
/// the <c>TabBar</c> as a <c>SelectorNavigator</c>, selecting items by Region.Name. The
/// sibling visibility-panel navigator materializes a FrameView per navigated route, identical
/// to the region test in <see cref="HotReloadRegionPage"/>.
/// </summary>
public sealed partial class HotReloadTabBarPage : Page
{
	public Grid ContentGrid { get; }
	public TabBar TabBar { get; }

	public HotReloadTabBarPage()
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

		outerGrid.Children.Add(TabBar);
		Content = outerGrid;
	}
}
