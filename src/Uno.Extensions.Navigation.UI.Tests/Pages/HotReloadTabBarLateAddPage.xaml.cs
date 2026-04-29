using Microsoft.UI.Xaml.Controls;
using Uno.Toolkit.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

public sealed partial class HotReloadTabBarLateAddPage : Page
{
	public HotReloadTabBarLateAddPage()
	{
		this.InitializeComponent();
	}

	public Grid? ContentGrid => (Grid?)FindName("_contentGrid");
	public TabBar? TabBar => (TabBar?)FindName("TB");
}
