namespace TestHarness.Ext.Http.Kiota;

[TestSectionRoot("Http: Kiota", TestSections.Http_Kiota, typeof(KiotaHostInit))]
public sealed partial class KiotaMainPage : BaseTestSectionPage
{
	public KiotaMainPage()
	{
		this.InitializeComponent();
	}

	public async void KiotaPageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<KiotaHomeViewModel>(this);
	}
}
