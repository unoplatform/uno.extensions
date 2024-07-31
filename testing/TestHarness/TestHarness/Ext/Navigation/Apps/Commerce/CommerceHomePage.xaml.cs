namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceHomePage : Page
{
	public CommerceHomeViewModel? ViewModel => DataContext as CommerceHomeViewModel;
	public CommerceHomePage()
	{
		this.InitializeComponent();
	}
}
