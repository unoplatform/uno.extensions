namespace TestHarness.Ext.Navigation.Apps.Commerce;

public sealed partial class CommerceProductsPage : Page
{
	public CommerceProductsViewModel? ViewModel => DataContext as CommerceProductsViewModel;

	public CommerceProductsPage()
	{
		this.InitializeComponent();

		
	}
}
