using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Panel page for #2904 test: visibility navigation with Region.Names.
/// XAML HR swaps Region.Name values to verify the panel doesn't go blank.
/// </summary>
public sealed partial class HotReloadPanelRegionNamesPage : Page
{
	public HotReloadPanelRegionNamesPage()
	{
		InitializeComponent();
	}

	public Grid ContentGrid => _contentGrid;
}
