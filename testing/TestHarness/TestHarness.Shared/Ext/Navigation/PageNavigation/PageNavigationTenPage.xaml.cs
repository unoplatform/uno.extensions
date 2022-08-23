
namespace TestHarness.Ext.Navigation.PageNavigation;

public sealed partial class PageNavigationTenPage : Page
{
	public PageNavigationTenViewModel? ViewModel => DataContext as PageNavigationTenViewModel;
	public PageNavigationTenPage()
	{
		this.InitializeComponent();
	}



}
