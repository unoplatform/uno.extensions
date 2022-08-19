
namespace TestHarness.Ext.Navigation.PageNavigation;

public sealed partial class PageNavigationSevenPage : Page
{
	public PageNavigationSevenViewModel? ViewModel => DataContext as PageNavigationSevenViewModel;
	public PageNavigationSevenPage()
	{
		this.InitializeComponent();
	}

}
