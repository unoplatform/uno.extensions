

namespace TestHarness.Ext.Authentication.MSAL;

public partial class MsalAuthenticationHomeViewModel : ObservableObject
{
	public INavigator Navigator { get; init; }
	public IAuthenticationFlow Flow { get; init; }

	public IMsalAuthenticationTaskListEndpoint TaskEndpoint { get; init; }

	public ITokenCache Tokens { get; }

	[ObservableProperty]
	private MsalAuthenticationToDoTaskListData[]? tasks;

	public MsalAuthenticationHomeViewModel(
		INavigator navigator,
		IAuthenticationFlow flow,
		IMsalAuthenticationTaskListEndpoint taskEndpoint,
		ITokenCache tokens)
	{
		Navigator = navigator;
		Flow = flow;
		TaskEndpoint = taskEndpoint;
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
		await Tokens.SaveAsync(creds);
	}

	public async Task RetrieveTasks()
	{
		var tasksResponse = await TaskEndpoint.GetAllAsync(CancellationToken.None);
		Tasks = tasksResponse?.Value?.ToArray();
	}
}
