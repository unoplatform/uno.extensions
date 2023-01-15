
namespace TestHarness.Ext.Navigation.NavigationView;

public sealed partial class NavigationViewDataBoundPage : Page
{
	public NavigationViewDataBoundViewModel? ViewModel => DataContext as NavigationViewDataBoundViewModel;
	public NavigationViewDataBoundPage()
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

	private void NavigationViewItemChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		CurrentNavigationViewItemText.Text = (args.SelectedItem as NavigationViewItem)?.Content + string.Empty;
	}
}
