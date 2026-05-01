using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// XAML-backed host page for testing Region.Navigator switching via HR.
/// XAML HR modifies Region.Navigator on the inner grid.
/// </summary>
public sealed partial class HotReloadRegionNavigatorPage : Page
{
	public HotReloadRegionNavigatorPage()
	{
		this.InitializeComponent();
	}

	public Grid OuterGrid => (Grid)FindName("_outerGrid");
	public Grid InnerGrid => (Grid)FindName("_innerGrid");
}
