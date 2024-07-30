namespace TestHarness.Ext.Authentication.Custom;

[ReactiveBindable(false)]
public partial class CustomAuthenticationHomeTestBackendViewModel : ObservableObject
{
	
	public INavigator Navigator { get; init; }
	public IAuthenticationService Authentication { get; init; }

	public ICustomAuthenticationTestBackendEndpoint Endpoint { get; init; }

	[ObservableProperty]
	private string[]? items;

	[ObservableProperty]
	private string? retrieveProductsResult;
	private ITokenCache Tokens { get; }


	public CustomAuthenticationHomeTestBackendViewModel(
		INavigator navigator,
		IAuthenticationService auth,
		ICustomAuthenticationTestBackendEndpoint endpoint,
		ITokenCache tokens)
	{
		Navigator = navigator;
		Authentication = auth;
		Endpoint = endpoint;
		Tokens = tokens;
	}

	public async void Logout()
	{
		await Authentication.LogoutAsync(CancellationToken.None);
		await Navigator.NavigateViewModelAsync<CustomAuthenticationLoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
	}

	public async void ClearAccessToken()
	{
		var creds = await Tokens.GetAsync(CancellationToken.None);
		creds.Remove(TokenCacheExtensions.AccessTokenKey);
		await Tokens.SaveAsync(await Tokens.GetCurrentProviderAsync(CancellationToken.None) ?? string.Empty, creds, CancellationToken.None);
	}

	public async void ClearAllTokens()
	{
		var creds = await Tokens.GetAsync(CancellationToken.None);
		creds.Remove(TokenCacheExtensions.AccessTokenKey);
		creds.Remove(TokenCacheExtensions.RefreshTokenKey);
		await Tokens.SaveAsync(await Tokens.GetCurrentProviderAsync(CancellationToken.None) ?? string.Empty, creds, CancellationToken.None);
	}

	public async void InvalidateTokens()
	{
		var creds = await Tokens.GetAsync(CancellationToken.None);
		creds[TokenCacheExtensions.AccessTokenKey]= $"Some_invalid_access_token_{DateTime.Now.Ticks}";
		creds[TokenCacheExtensions.RefreshTokenKey] = $"Some_invalid_refresh_token_{DateTime.Now.Ticks}"; 
		await Tokens.SaveAsync(await Tokens.GetCurrentProviderAsync(CancellationToken.None) ?? string.Empty, creds, CancellationToken.None);
	}
	

	public async void Retrieve()
	{
		try
		{
			var response = await Endpoint.GetDataAuthorizationHeader(CancellationToken.None);
			Items = response?.ToArray();
			RetrieveProductsResult = Constants.CommerceProducts.ProductsLoadSuccess;
		}
		catch
		{
			RetrieveProductsResult = Constants.CommerceProducts.ProductsLoadError;
		}
	}
	public async void RetrieveCookie()
	{
		try
		{
			var response = await Endpoint.GetDataCookie(CancellationToken.None);
			Items = response?.ToArray();
			RetrieveProductsResult = Constants.CommerceProducts.ProductsLoadSuccess;
		}
		catch
		{
			RetrieveProductsResult = Constants.CommerceProducts.ProductsLoadError;
		}
	}
}
