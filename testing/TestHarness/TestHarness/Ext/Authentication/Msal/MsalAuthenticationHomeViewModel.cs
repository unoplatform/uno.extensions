namespace TestHarness.Ext.Authentication.MSAL;

[ReactiveBindable(false)]
public partial class MsalAuthenticationHomeViewModel : ObservableObject
{
	private readonly IDispatcher _dispatcher;
	private readonly INavigator _navigator;
	private readonly IAuthenticationService _authentication;

	private readonly IMsalAuthenticationTaskListEndpoint _taskEndpoint;
	private readonly ICustomAuthenticationDummyJsonEndpoint? _endpoint;

	private readonly ITokenCache _tokens;

	[ObservableProperty]
	private MsalAuthenticationToDoTaskListData[]? tasks;


	[ObservableProperty]
	private CustomAuthenticationProduct[]? products;

	public MsalAuthenticationHomeViewModel(
		IDispatcher dispatcher,
		INavigator navigator,
		IAuthenticationService auth,
		ITokenCache tokens,
		IMsalAuthenticationTaskListEndpoint taskEndpoint,
		ICustomAuthenticationDummyJsonEndpoint? endpoint=null)
	{
		_dispatcher = dispatcher;
		_navigator = navigator;
		_authentication = auth;
		_taskEndpoint = taskEndpoint;
		_tokens = tokens;
		_endpoint = endpoint;
	}

	public async void Logout()
	{
		await _authentication.LogoutAsync(_dispatcher, CancellationToken.None);
		await _navigator.NavigateViewModelAsync<MsalAuthenticationWelcomeViewModel>(this, qualifier: Qualifiers.ClearBackStack);
	}

	public async void ClearAccessToken()
	{
		var creds = await _tokens.GetAsync(CancellationToken.None);
		creds.Remove(TokenCacheExtensions.AccessTokenKey);
		await _tokens.SaveAsync(await _tokens.GetCurrentProviderAsync(CancellationToken.None) ?? string.Empty, creds, CancellationToken.None);
	}

	public async void Retrieve()
	{
		var current = await _tokens.GetCurrentProviderAsync(CancellationToken.None);
		if (current?.StartsWith("Custom") ?? false)
		{
			var response = await _endpoint!.Products(CancellationToken.None);
			Products = response?.Products?.ToArray();
		}
		else
		{
			var tasksResponse = await _taskEndpoint.GetAllAsync(CancellationToken.None);
			Tasks = tasksResponse?.Value?.ToArray();
		}
	}
}
