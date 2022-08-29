namespace TestHarness.Ext.Authentication;

internal record AuthenticationShellViewModel
{
	private readonly IAuthenticationService _authentication;
	private readonly INavigator _navigator;
	private readonly IAuthenticationRouteInfo _routing;

	public AuthenticationShellViewModel(
		IAuthenticationService auth,
		INavigator navigator,
		IAuthenticationRouteInfo routing)
	{
		_authentication = auth;
		_navigator = navigator;
		_routing = routing;

		_ = Start();
	}

	private async Task Start()
	{
		var authenticated = await _authentication.RefreshAsync();
		if (authenticated)
		{
			await _navigator.NavigateViewModelAsync(this, _routing.HomeViewModel);
		}
		else
		{
			await _navigator.NavigateViewModelAsync(this, _routing.LoginViewModel);
		}
	}
}
