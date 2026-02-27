using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.TabNav;

/// <summary>
/// Page with a tabbed layout:
///   OuterGrid (Region.Attached, composite)
///     ├── ContentGrid (Region.Attached, Region.Navigator="Visibility") → PanelVisibilityNavigator
///     └── NavigationView (Region.Attached) → NavigationViewNavigator (SelectorNavigator)
///           ├── NavigationViewItem Region.Name="TabA"
///           └── NavigationViewItem Region.Name="TabB"
///
/// This replicates the Chefs MainPage tab structure using NavigationView
/// instead of TabBar (which requires the Toolkit dependency).
/// </summary>
public sealed partial class TabbedMainPage : Page
{
	/// <summary>The content area Grid (PanelVisibilityNavigator).</summary>
	public Grid ContentGrid { get; }

	/// <summary>The NavigationView acting as the tab selector.</summary>
	public NavigationView TabSelector { get; }

	public TabbedMainPage()
	{
		var outerGrid = new Grid();
		outerGrid.RowDefinitions.Add(new RowDefinition());
		outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		Region.SetAttached(outerGrid, true);

		// Content area — becomes PanelVisibilityNavigator
		ContentGrid = new Grid();
		Region.SetAttached(ContentGrid, true);
		Region.SetNavigator(ContentGrid, "Visibility");
		Grid.SetRow(ContentGrid, 0);
		outerGrid.Children.Add(ContentGrid);

		// Selector — NavigationView with tab items
		TabSelector = new NavigationView
		{
			PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
			IsSettingsVisible = false,
			IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed
		};
		Region.SetAttached(TabSelector, true);

		var tabA = new NavigationViewItem { Content = "Tab A" };
		Region.SetName(tabA, "TabA");
		TabSelector.MenuItems.Add(tabA);

		var tabB = new NavigationViewItem { Content = "Tab B" };
		Region.SetName(tabB, "TabB");
		TabSelector.MenuItems.Add(tabB);

		Grid.SetRow(TabSelector, 1);
		outerGrid.Children.Add(TabSelector);

		Content = outerGrid;
	}
}
