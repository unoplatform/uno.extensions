namespace TestHarness.Ext.Authentication.Custom;

[ReactiveBindable(false)]
public partial class CustomAuthenticationHomeTestBackendViewModel : ObservableObject
{
	
	public INavigator Navigator { get; init; }
	public IAuthenticationFlow Flow { get; init; }

	public ICustomAuthenticationTestBackendEndpoint Endpoint { get; init; }

	[ObservableProperty]
	private string[]? items;

	[ObservableProperty]
	private string? retrieveProductsResult;
	private ITokenCache Tokens { get; }


	public CustomAuthenticationHomeTestBackendViewModel(
		INavigator navigator,
		IAuthenticationFlow flow,
		ICustomAuthenticationTestBackendEndpoint endpoint,
		ITokenCache tokens)
	{
		Navigator = navigator;
		Flow = flow;
		Endpoint = endpoint;
		Tokens = tokens;
	}

	public async void Logout()
	{
		await Flow.LogoutAsync(CancellationToken.None);
	}

	public async void ClearAccessToken()
	{
		var creds = await Tokens.GetAsync();
		creds.Remove(TokenCacheExtensions.AccessTokenKey);
		await Tokens.SaveAsync(Tokens.CurrentProvider ?? string.Empty, creds);
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
