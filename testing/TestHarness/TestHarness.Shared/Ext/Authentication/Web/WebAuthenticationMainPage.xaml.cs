namespace TestHarness.Ext.Authentication.Web;

[TestSectionRoot("Authentication: Web",TestSections.Authentication_Web, typeof(WebAuthenticationHostInit))]
public sealed partial class WebAuthenticationMainPage : BaseTestSectionPage
{
	public WebAuthenticationMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.NavigateRouteAsync(this,"");
	}
}
