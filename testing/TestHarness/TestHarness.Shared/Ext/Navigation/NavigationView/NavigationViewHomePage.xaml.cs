
namespace TestHarness.Ext.Navigation.NavigationView;

public sealed partial class NavigationViewHomePage : Page
{
	public NavigationViewHomePage()
	{
		this.InitializeComponent();
	}

	public void NavigationItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs e)
	{
		if(e.InvokedItemContainer == sender.SettingsItem as Microsoft.UI.Xaml.Controls.NavigationViewItem)
		{
			this.Navigator()!.NavigateViewModelAsync<NavigationViewSettingsViewModel>(this);
		}
	}
}
