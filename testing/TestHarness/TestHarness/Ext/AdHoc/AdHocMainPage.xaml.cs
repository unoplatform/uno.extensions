namespace TestHarness.Ext.AdHoc;

[TestSectionRoot("AdHoc",TestSections.AdHoc, typeof(AdHocHostInit))]
public sealed partial class AdHocMainPage : BaseTestSectionPage
{
	public AdHocMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<AdHocOneViewModel>(this);
	}

}
