using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Page for the Region.Name add/remove test (#3075).
/// One child starts without a Region.Name; XAML HR adds one.
/// </summary>
public sealed partial class HotReloadRegionNameAddRemovePage : Page
{
	public HotReloadRegionNameAddRemovePage()
	{
		InitializeComponent();
	}

	public Grid ContentGrid => _contentGrid;
}
