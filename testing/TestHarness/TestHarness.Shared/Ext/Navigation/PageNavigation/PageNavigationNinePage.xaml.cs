
namespace TestHarness.Ext.Navigation.PageNavigation;

public sealed partial class PageNavigationNinePage : Page
{
	public PageNavigationNineViewModel? ViewModel => DataContext as PageNavigationNineViewModel;
	public PageNavigationNinePage()
	{
		this.InitializeComponent();
	}


}
