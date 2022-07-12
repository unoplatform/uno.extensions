
namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceDealsPage : Page
{
	public CommerceDealsViewModel? ViewModel => DataContext as CommerceDealsViewModel;
	public CommerceDealsPage()
	{
		this.InitializeComponent();
	}

}
