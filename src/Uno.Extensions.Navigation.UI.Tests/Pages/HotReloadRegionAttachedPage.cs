using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Host page with a Region.Attached grid that can be toggled via XAML HR.
/// Used to test adding/removing Region.Attached on panels.
/// </summary>
public sealed partial class HotReloadRegionAttachedPage : Page
{
	public Grid ContentGrid { get; }
	public Grid InnerGrid { get; }

	public HotReloadRegionAttachedPage()
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
