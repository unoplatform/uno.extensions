namespace TestHarness.Ext.Navigation.ListToDetails;

[TestSectionRoot("ListToDetails Navigation",TestSections.Navigation_ListToDetails, typeof(ListToDetailsHostInit))]
public sealed partial class ListToDetailsMainPage : BaseTestSectionPage
{
	public ListToDetailsMainPage()
	{
		this.InitializeComponent();
	}

	public async void ListToDetailsHomeClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<ListToDetailsHomeViewModel>(this);
	}
}
