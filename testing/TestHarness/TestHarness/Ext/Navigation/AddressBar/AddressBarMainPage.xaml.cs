namespace TestHarness.Ext.Navigation.AddressBar;

[TestSectionRoot("AddressBar Navigation", TestSections.Navigation_AddressBar, typeof(AddressBarHostInit))]
[TestSectionRoot("AddressBar Navigation (Nested)", TestSections.Navigation_AddressBar_Nested, typeof(AddressBarNestedHostInit))]
[TestSectionRoot("AddressBar Navigation (Nested) (Default Nav)", TestSections.Navigation_AddressBar_Nested_Default, typeof(AddressBarNestedDefaultHostInit))]
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
