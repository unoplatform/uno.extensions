namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceProductDetailsPage : Page
{
	public CommerceProductDetailsViewModel? ViewModel => DataContext as CommerceProductDetailsViewModel;

	public CommerceProductDetailsPage()
	{
		this.InitializeComponent();
	}
}
