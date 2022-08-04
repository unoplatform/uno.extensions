namespace TestHarness.Ext.Authentication.Oidc;

public partial class OidcAuthenticationHomeViewModel : ObservableObject
{
	public INavigator Navigator { get; init; }
	public IAuthenticationFlow Flow { get; init; }

	public IOidcAuthenticationTestEndpoint Endpoint { get; init; }

	[ObservableProperty]
	private OidcAuthenticationTestItem[]? items;

	public OidcAuthenticationHomeViewModel(INavigator navigator, IAuthenticationFlow flow, IOidcAuthenticationTestEndpoint endpoint)
	{
		Navigator = navigator;
		Flow = flow;
		Endpoint = endpoint;
	}

	public async Task Logout()
	{
		await Flow.LogoutAsync(CancellationToken.None);
	}

	public async Task Retrieve()
	{
		var response = await Endpoint.Test(CancellationToken.None);
		Items= response;
	}
}
