using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.NavViewShell;

/// <summary>
/// Shell page replicating the NavigationView layout emitted by app scaffolders,
/// where the visibility content grid is nested INSIDE NavigationView.Content
/// (unlike <c>TabbedMainPage</c> where the content grid is a sibling):
///   OuterGrid (Region.Attached, composite)
///     └── NavigationView (Region.Attached) → NavigationViewNavigator (SelectorNavigator)
///           ├── NavigationViewItem Region.Name="NavHome"   [IsDefault route]
///           ├── NavigationViewItem Region.Name="NavMenu"
///           ├── NavigationViewItem Region.Name="NavOrders"
///           └── Content: ContentGrid (Region.Attached, Region.Navigator="Visibility")
/// </summary>
public sealed partial class NavViewShellPage : Page
{
	/// <summary>The content area Grid (PanelVisibilityNavigator), nested in NavigationView.Content.</summary>
	public Grid ContentGrid { get; }

	/// <summary>The NavigationView acting as the tab selector.</summary>
	public NavigationView NavView { get; }

	public NavigationViewItem HomeItem { get; }
	public NavigationViewItem MenuItem { get; }
	public NavigationViewItem OrdersItem { get; }

	public NavViewShellPage()
	{
		var outerGrid = new Grid();
		Region.SetAttached(outerGrid, true);

		NavView = new NavigationView
		{
			PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
			IsSettingsVisible = false,
			IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed
		};
		Region.SetAttached(NavView, true);

		HomeItem = new NavigationViewItem { Content = "Home" };
		Region.SetName(HomeItem, "NavHome");
		NavView.MenuItems.Add(HomeItem);

		MenuItem = new NavigationViewItem { Content = "Menu" };
		Region.SetName(MenuItem, "NavMenu");
		NavView.MenuItems.Add(MenuItem);

		OrdersItem = new NavigationViewItem { Content = "Orders" };
		Region.SetName(OrdersItem, "NavOrders");
		NavView.MenuItems.Add(OrdersItem);

		// Content area nested inside the NavigationView, matching the scaffolded shell layout
		ContentGrid = new Grid();
		Region.SetAttached(ContentGrid, true);
		Region.SetNavigator(ContentGrid, "Visibility");
		NavView.Content = ContentGrid;

		outerGrid.Children.Add(NavView);
		Content = outerGrid;
	}
}
