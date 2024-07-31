namespace TestHarness.Ext.Navigation.Storage;

[TestSectionRoot("Storage",TestSections.Core_Storage, typeof(StorageHostInit))]
public sealed partial class StorageMainPage : BaseTestSectionPage
{
	public StorageMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<StorageOneViewModel>(this);
	}

}
