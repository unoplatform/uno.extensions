using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Page for the region rename test (#3088). Has a TabBar with two items
/// whose Region.Name values are changed via XAML HR.
/// </summary>
public sealed partial class HotReloadRegionRenamePage : Page
{
	public HotReloadRegionRenamePage()
	{
		InitializeComponent();
	}

	public Grid ContentGrid => _contentGrid;
}
