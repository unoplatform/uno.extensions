namespace TestHarness.Ext.Authentication.MSAL;

[ReactiveBindable(false)]
public partial class MsalAuthenticationHomeViewModel : ObservableObject
{
	public INavigator Navigator { get; init; }
	public IAuthenticationService Authentication { get; init; }

	public IMsalAuthenticationTaskListEndpoint TaskEndpoint { get; init; }
	public ICustomAuthenticationDummyJsonEndpoint? Endpoint { get; init; }

	public ITokenCache Tokens { get; }

	[ObservableProperty]
	private MsalAuthenticationToDoTaskListData[]? tasks;


	[ObservableProperty]
	private CustomAuthenticationProduct[]? products;

	public MsalAuthenticationHomeViewModel(
		INavigator navigator,
		IAuthenticationService auth,
		ITokenCache tokens,
		IMsalAuthenticationTaskListEndpoint taskEndpoint,
		ICustomAuthenticationDummyJsonEndpoint? endpoint=null)
	{
		Navigator = navigator;
		Authentication = auth;
		TaskEndpoint = taskEndpoint;
		Tokens = tokens;
		Endpoint = endpoint;
	}

	public async void Logout()
	{
		await Authentication.LogoutAsync(CancellationToken.None);
		await Navigator.NavigateViewModelAsync<MsalAuthenticationWelcomeViewModel>(this, qualifier: Qualifiers.ClearBackStack);
	}

	public async void ClearAccessToken()
	{
		var creds = await Tokens.GetAsync(CancellationToken.None);
		creds.Remove(TokenCacheExtensions.AccessTokenKey);
		await Tokens.SaveAsync(await Tokens.CurrentProviderAsync(CancellationToken.None) ?? string.Empty, creds, CancellationToken.None);
	}

	public async void Retrieve()
	{
		var current = await Tokens.CurrentProviderAsync(CancellationToken.None);
		if (current?.StartsWith("Custom") ?? false)
		{
			var response = await Endpoint!.Products(CancellationToken.None);
			Products = response?.Products?.ToArray();
		}
		else
		{
			var tasksResponse = await TaskEndpoint.GetAllAsync(CancellationToken.None);
			Tasks = tasksResponse?.Value?.ToArray();
		}
	}
}
