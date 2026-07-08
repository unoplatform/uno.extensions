using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Host page for the region-based HR test. Layout:
///   OuterGrid (Region.Attached)
///     └── ContentGrid (Region.Attached, Region.Navigator="Visibility") → PanelVisibilityNavigator
/// ContentGrid is intentionally empty — the PanelVisibilityNavigator creates a FrameView child
/// per navigated route (see PanelVisiblityNavigator.Show), sets Region.Name on it to match the
/// route name, and toggles Visibility between previously-created children on subsequent nav.
/// </summary>
public sealed partial class HotReloadRegionPage : Page
{
	public Grid ContentGrid { get; }

	public HotReloadRegionPage()
	{
		var outerGrid = new Grid();
		Region.SetAttached(outerGrid, true);

		ContentGrid = new Grid();
		Region.SetAttached(ContentGrid, true);
		Region.SetNavigator(ContentGrid, "Visibility");
		outerGrid.Children.Add(ContentGrid);

		Content = outerGrid;
	}
}
