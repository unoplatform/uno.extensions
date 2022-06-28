namespace TestHarness.Ext.Authentication.Custom;

public partial class CustomAuthenticationHomeViewModel : ObservableObject
{
	public INavigator Navigator { get; init; }
	public IAuthenticationFlow Flow { get; init; }

	public ICustomAuthenticationDummyJsonEndpoint Endpoint { get; init; }

	[ObservableProperty]
	private CustomAuthenticationProduct[]? products;

	public CustomAuthenticationHomeViewModel(INavigator navigator, IAuthenticationFlow flow, ICustomAuthenticationDummyJsonEndpoint endpoint)
	{
		Navigator = navigator;
		Flow = flow;
		Endpoint = endpoint;
	}

	public async Task Logout()
	{
		await Flow.LogoutAsync(CancellationToken.None);
	}

	public async Task RetrieveProducts()
	{
		var response = await Endpoint.Products(CancellationToken.None);
		Products = response?.Products?.ToArray();
	}
}
