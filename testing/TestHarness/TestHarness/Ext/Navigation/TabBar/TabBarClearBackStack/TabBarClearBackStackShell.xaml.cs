using Uno.Extensions.Hosting;

namespace TestHarness.Ext.Navigation.TabBar.TabBarClearBackStack;

public sealed partial class TabBarClearBackStackShell : UserControl, IContentControlProvider
{
	public TabBarClearBackStackShell()
	{
		this.InitializeComponent();
	}

	public Microsoft.UI.Xaml.Controls.ContentControl ContentControl => Splash;
}
