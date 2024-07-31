namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceProfilePage : Page
{
	public CommerceProfileViewModel? ViewModel => DataContext as CommerceProfileViewModel;

	public CommerceProfilePage()
	{
		this.InitializeComponent();
	}
}
