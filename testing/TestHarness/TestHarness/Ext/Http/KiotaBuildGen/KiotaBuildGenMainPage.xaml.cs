namespace TestHarness.Ext.Http.KiotaBuildGen;

[TestSectionRoot("Http: Kiota BuildGen", TestSections.Http_Kiota_BuildGen, typeof(KiotaBuildGenHostInit))]
public sealed partial class KiotaBuildGenMainPage : BaseTestSectionPage
{
	public KiotaBuildGenMainPage()
	{
		this.InitializeComponent();
	}

	public async void BuildGenHomeClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<KiotaBuildGenHomeViewModel>(this);
	}
}
