
namespace TestHarness.Ext.Authentication.Oidc;

public record OidcAuthenticationShellViewModel
{
	private readonly INavigator _navigator;
	private readonly IAuthenticationFlow _flow;
	public OidcAuthenticationShellViewModel(IAuthenticationFlow flow, INavigator navigator, IDispatcher dispatcher)
	{
		_navigator=navigator;
		_flow = flow;

		flow.Initialize(dispatcher, navigator);

		_ = Launch();
	}


	private async Task Launch()
	{
		var authenticated = await _flow.EnsureAuthenticatedAsync(CancellationToken.None);
		if(authenticated)
		{
			await _navigator.NavigateViewModelAsync<OidcAuthenticationHomeViewModel>(this);
		}
	}
}
