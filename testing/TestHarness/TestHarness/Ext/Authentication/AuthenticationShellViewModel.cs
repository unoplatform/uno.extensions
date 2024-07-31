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
		_authentication.LoggedOut += _authentication_LoggedOut;
		_navigator = navigator;
		_routing = routing;

		_ = Start();
	}

	private async void _authentication_LoggedOut(object? sender, EventArgs e)
	{
		await _navigator.NavigateViewModelAsync(this, _routing.LoginViewModel, qualifier: Qualifiers.ClearBackStack);
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
