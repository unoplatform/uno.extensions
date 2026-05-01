using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// XAML-backed host page with a Region.Attached grid.
/// XAML HR modifies Region.Attached on the inner grid.
/// </summary>
public sealed partial class HotReloadRegionAttachedPage : Page
{
	public HotReloadRegionAttachedPage()
	{
		this.InitializeComponent();
	}

	public Grid OuterGrid => (Grid)FindName("_outerGrid");
	public Grid InnerGrid => (Grid)FindName("_innerGrid");
}
