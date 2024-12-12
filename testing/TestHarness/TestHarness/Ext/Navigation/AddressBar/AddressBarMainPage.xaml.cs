namespace TestHarness.Ext.Navigation.AddressBar;

[TestSectionRoot("AddressBar Navigation", TestSections.Navigation_AddressBar, typeof(AddressBarHostInit))]
public sealed partial class AddressBarMainPage : BaseTestSectionPage
{
	public AddressBarMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateRouteAsync(this, "");
	}
}
