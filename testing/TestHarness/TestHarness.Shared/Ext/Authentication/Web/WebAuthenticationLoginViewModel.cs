namespace TestHarness.Ext.Authentication.Web;

internal record class WebAuthenticationLoginViewModel(INavigator Navigator, IAuthenticationService Authentication, IAuthenticationRouteInfo RouteInfo)
{
	public async void Login()
	{
		var authenticated = await Authentication.LoginAsync(new Dictionary<string, string>(){ { "LoginMetaData", "SocialPlatform" } });
		if (authenticated)
		{
			await Navigator.NavigateViewModelAsync(this, RouteInfo.HomeViewModel, qualifier: Qualifiers.ClearBackStack);
		}
	}
}
