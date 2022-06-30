namespace TestHarness.Ext.Authentication.Custom;

public partial class CustomAuthenticationHomeViewModel : ObservableObject
{
	
	public INavigator Navigator { get; init; }
	public IAuthenticationFlow Flow { get; init; }

	public ICustomAuthenticationDummyJsonEndpoint Endpoint { get; init; }

	[ObservableProperty]
	private CustomAuthenticationProduct[]? products;

	[ObservableProperty]
	private string? retrieveProductsResult;
	private ITokenCache Tokens { get; }


	public CustomAuthenticationHomeViewModel(
		INavigator navigator,
		IAuthenticationFlow flow,
		ICustomAuthenticationDummyJsonEndpoint endpoint,
		ITokenCache tokens)
	{
		Navigator = navigator;
		Flow = flow;
		Endpoint = endpoint;
		Tokens = tokens;
	}

	public async Task Logout()
	{
		await Flow.LogoutAsync(CancellationToken.None);
	}

	public async Task ClearAccessToken()
	{
		var creds = await Tokens.GetAsync();
		creds.Remove(TokenCacheExtensions.AccessTokenKey);
		await Tokens.SaveAsync(Tokens.CurrentProvider ?? string.Empty, creds);
	}

	public async Task RetrieveProducts()
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
