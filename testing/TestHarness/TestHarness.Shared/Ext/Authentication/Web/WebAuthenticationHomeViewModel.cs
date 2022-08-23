namespace TestHarness.Ext.Authentication.Web;

[ReactiveBindable(false)]
public partial class WebAuthenticationHomeViewModel : ObservableObject
{
	public INavigator Navigator { get; init; }
	public IAuthenticationFlow Flow { get; init; }

	public IWebAuthenticationTestEndpoint Endpoint { get; init; }

	[ObservableProperty]
	private string[]? items;

	public WebAuthenticationHomeViewModel(INavigator navigator, IAuthenticationFlow flow, IWebAuthenticationTestEndpoint endpoint)
	{
		Navigator = navigator;
		Flow = flow;
		Endpoint = endpoint;
	}

	public async void Logout()
	{
		await Flow.LogoutAsync(CancellationToken.None);
	}

	public async void Retrieve()
	{
		var response = await Endpoint.GetDataFacebook(CancellationToken.None);
		Items = response?.ToArray();
	}
}
