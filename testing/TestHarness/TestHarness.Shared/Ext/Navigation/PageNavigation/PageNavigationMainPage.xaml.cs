using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace TestHarness.Ext.Navigation.PageNavigation;

[TestSectionRoot("PageNavigation", typeof(PageNavigationHostInit))]
public sealed partial class PageNavigationMainPage : Page
{
	public PageNavigationMainPage()
	{
		this.InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		if (e.Parameter is IHostInitialization hostInit)
		{
			var host = hostInit.InitializeHost();
			this.AttachServiceProvider(host.Services).RegisterWindow((Application.Current as App)!.Window);
			Region.SetAttached(Root, true);
		}
	}

	public async void TestClick(object sender, RoutedEventArgs e)
	{
		await Root.Navigator()!.ShowMessageDialogAsync(this, "Confirm");
	}

}
