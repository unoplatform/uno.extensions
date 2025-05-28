
using Uno.Extensions.Navigation.UI;

namespace TestHarness.Ext.Navigation.NavigationView;

public sealed partial class NavigationViewHomePage : Page
{
	public NavigationViewHomePage()
	{
		this.InitializeComponent();
		this.Loaded += NavigationViewHomePage_Loaded;
	}

	private void NavigationViewHomePage_Loaded(object sender, RoutedEventArgs e)
	{
		var item = (NavigationViewItem)NavView.SettingsItem;
		Region.SetName(item, "Settings");
	}

	private void NavigationViewItemChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		CurrentNavigationViewItemText.Text = (args.SelectedItem as NavigationViewItem)?.Content + string.Empty;
	}
}
