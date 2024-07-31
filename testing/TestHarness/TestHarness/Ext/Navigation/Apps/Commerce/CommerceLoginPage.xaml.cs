namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceLoginPage : Page
{
	internal CommerceLoginViewModel? ViewModel => DataContext as CommerceLoginViewModel;

	public CommerceLoginPage()
	{
		this.InitializeComponent();
	}
}
