namespace TestHarness.Ext.Authentication.MSAL;

[ReactiveBindable(false)]
public partial class MsalAuthenticationHomeViewModel : ObservableObject
{
	public INavigator Navigator { get; init; }
	public IAuthenticationFlow Flow { get; init; }

	public IMsalAuthenticationTaskListEndpoint TaskEndpoint { get; init; }
	public ICustomAuthenticationDummyJsonEndpoint? Endpoint { get; init; }

	public ITokenCache Tokens { get; }

	[ObservableProperty]
	private MsalAuthenticationToDoTaskListData[]? tasks;


	[ObservableProperty]
	private CustomAuthenticationProduct[]? products;

	public MsalAuthenticationHomeViewModel(
		INavigator navigator,
		IAuthenticationFlow flow,
		ITokenCache tokens,
		IMsalAuthenticationTaskListEndpoint taskEndpoint,
		ICustomAuthenticationDummyJsonEndpoint? endpoint=null)
	{
		Navigator = navigator;
		Flow = flow;
		TaskEndpoint = taskEndpoint;
		Tokens = tokens;
		Endpoint = endpoint;
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
		if (Tokens.CurrentProvider?.StartsWith("Custom") ?? false)
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
