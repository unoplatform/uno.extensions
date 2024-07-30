namespace TestHarness.Ext.Authentication.Web;

[ReactiveBindable(false)]
public partial class WebAuthenticationHomeViewModel : ObservableObject
{
	public INavigator Navigator { get; init; }
	public IAuthenticationService Authentication { get; init; }

	public IWebAuthenticationTestEndpoint Endpoint { get; init; }

	[ObservableProperty]
	private string[]? items;

	public WebAuthenticationHomeViewModel(INavigator navigator, IAuthenticationService auth, IWebAuthenticationTestEndpoint endpoint)
	{
		Navigator = navigator;
		Authentication = auth;
		Endpoint = endpoint;
	}

	public async void Logout()
	{
		await Authentication.LogoutAsync(CancellationToken.None);
		await Navigator.NavigateViewModelAsync<WebAuthenticationLoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
	}

	public async void Retrieve()
	{
		var response = await Endpoint.GetDataFacebook(CancellationToken.None);
		Items = response?.ToArray();
	}
}
