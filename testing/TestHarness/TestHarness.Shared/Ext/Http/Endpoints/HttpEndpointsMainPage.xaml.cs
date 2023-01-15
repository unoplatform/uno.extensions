namespace TestHarness.Ext.Http.Endpoints;

[TestSectionRoot("HttpEndpoints",TestSections.Http_Endpoints, typeof(HttpEndpointsHostInit))]
public sealed partial class HttpEndpointsMainPage : BaseTestSectionPage
{
	public HttpEndpointsMainPage()
	{
		this.InitializeComponent();
	}

	public async void OnePageClick(object sender, RoutedEventArgs e)
	{
		await Navigator.NavigateViewModelAsync<HttpEndpointsOneViewModel>(this);
	}

}
