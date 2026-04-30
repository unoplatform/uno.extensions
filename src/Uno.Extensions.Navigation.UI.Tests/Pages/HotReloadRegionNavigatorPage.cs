using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Host page for testing Region.Navigator switching via HR.
/// The InnerGrid has Region.Navigator="Visibility" which can be
/// toggled/changed to test how the navigation responds.
/// </summary>
public sealed partial class HotReloadRegionNavigatorPage : Page
{
	public Grid ContentGrid { get; }
	public Grid InnerGrid { get; }

	public HotReloadRegionNavigatorPage()
	{
		ContentGrid = new Grid();
		Region.SetAttached(ContentGrid, true);

		InnerGrid = new Grid();
		Region.SetAttached(InnerGrid, true);
		Region.SetNavigator(InnerGrid, "Visibility");
		ContentGrid.Children.Add(InnerGrid);

		Content = ContentGrid;
	}
}
