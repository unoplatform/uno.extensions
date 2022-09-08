namespace TestHarness.Ext.Authentication.Custom;

[ReactiveBindable(false)]
public partial class CustomAuthenticationHomeViewModel : ObservableObject
{
	
	public INavigator Navigator { get; init; }
	public IAuthenticationService Authentication { get; init; }

	public ICustomAuthenticationDummyJsonEndpoint Endpoint { get; init; }

	[ObservableProperty]
	private CustomAuthenticationProduct[]? products;

	[ObservableProperty]
	private string? retrieveProductsResult;
	private ITokenCache Tokens { get; }


	public CustomAuthenticationHomeViewModel(
		INavigator navigator,
		IAuthenticationService auth,
		ICustomAuthenticationDummyJsonEndpoint endpoint,
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

	public async void RetrieveProducts()
	{
		try
		{
			var response = await Endpoint.Products(CancellationToken.None);
			Products = response?.Products?.ToArray();
			RetrieveProductsResult = Constants.CommerceProducts.ProductsLoadSuccess;
		}
		catch
		{
			RetrieveProductsResult = Constants.CommerceProducts.ProductsLoadError;
		}
	}
}
