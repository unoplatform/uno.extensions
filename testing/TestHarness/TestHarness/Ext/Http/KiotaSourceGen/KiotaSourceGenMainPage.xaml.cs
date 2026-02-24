namespace TestHarness.Ext.Http.KiotaSourceGen;

[TestSectionRoot("Http: Kiota SourceGen", TestSections.Http_Kiota_SourceGen, typeof(KiotaSourceGenHostInit))]
public sealed partial class KiotaSourceGenMainPage : BaseTestSectionPage
{
	public KiotaSourceGenMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowSourceGenHomeClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<KiotaSourceGenHomeViewModel>(this);
	}
}
