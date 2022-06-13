namespace TestHarness.Ext.Authentication.Custom;

[TestSectionRoot("Authentication: Custom",TestSections.Authentication_Custom, typeof(CustomAuthenticationHostInit))]
public sealed partial class CustomAuthenticationMainPage : BaseTestSectionPage
{
	public CustomAuthenticationMainPage()
	{
		this.InitializeComponent();
	}

	public async void ShowAppClick(object sender, RoutedEventArgs e)
	{
		await NavigationRoot.Navigator()!.NavigateRouteAsync(this,"");
	}
}
