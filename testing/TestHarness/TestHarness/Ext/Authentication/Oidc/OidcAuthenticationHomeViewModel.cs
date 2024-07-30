namespace TestHarness.Ext.Authentication.Oidc;

[ReactiveBindable(false)]
public partial class OidcAuthenticationHomeViewModel : ObservableObject
{
	public INavigator Navigator { get; init; }
	public IAuthenticationService Authentication { get; init; }

	public IOidcAuthenticationTestEndpoint Endpoint { get; init; }

	[ObservableProperty]
	private OidcAuthenticationTestItem[]? items;

	public OidcAuthenticationHomeViewModel(INavigator navigator, IAuthenticationService auth, IOidcAuthenticationTestEndpoint endpoint)
	{
		Navigator = navigator;
		Authentication = auth;
		Endpoint = endpoint;
	}

	public async void Logout()
	{
		await Authentication.LogoutAsync(CancellationToken.None);
		await Navigator.NavigateViewModelAsync<OidcAuthenticationLoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
	}

	public async void Retrieve()
	{
		var response = await Endpoint.Test(CancellationToken.None);
		Items= response;
	}
}
