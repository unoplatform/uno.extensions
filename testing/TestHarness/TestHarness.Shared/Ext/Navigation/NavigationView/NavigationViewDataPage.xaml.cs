
namespace TestHarness.Ext.Navigation.NavigationView;

public sealed partial class NavigationViewDataPage : Page
{
	public NavigationViewDataViewModel? ViewModel => DataContext as NavigationViewDataViewModel;
	public NavigationViewDataPage()
	{
		this.InitializeComponent();
	}

}
