
namespace TestHarness.Ext.Navigation.NavigationView;

public sealed partial class NavigationViewHomePage : Page
{
	public NavigationViewHomePage()
	{
		this.InitializeComponent();
	}

	public void NavigationItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, NavigationViewItemInvokedEventArgs e)
	{
		if(e.InvokedItemContainer == sender.SettingsItem)
		{
			this.Navigator().NavigateViewModelAsync<NavigationViewSettingsViewModel>(this);
		}
	}
}
