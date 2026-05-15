using Microsoft.UI.Xaml.Controls;
using Uno.Toolkit.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// "Main" host page used by the full-flow HR scenario in
/// <see cref="Given_TabBarHotReload"/>. Starts as a simple placeholder Grid;
/// XAML hot-reload injects a TabBar+content-region layout whose TabBarItems
/// map to <see cref="FirstPage"/> and <see cref="SecondPage"/>. Distinct from
/// <see cref="HotReloadTabBarLateAddPage"/> so the two late-add tests don't
/// race on the same .xaml file revert.
/// </summary>
public sealed partial class HotReloadMainTabBarPage : Page
{
	public HotReloadMainTabBarPage()
	{
		this.InitializeComponent();
	}

	public Grid? ContentGrid => (Grid?)FindName("_contentGrid");
	public TabBar? TabBar => (TabBar?)FindName("TB");
}
