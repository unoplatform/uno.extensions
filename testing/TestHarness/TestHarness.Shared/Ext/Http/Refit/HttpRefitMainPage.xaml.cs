namespace TestHarness.Ext.Http.Refit;

[TestSectionRoot("HttpRefit",TestSections.Http_Refit, typeof(HttpRefitHostInit))]
public sealed partial class HttpRefitMainPage : BaseTestSectionPage
{
	public HttpRefitMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<HttpRefitOneViewModel>(this);
	}

}
