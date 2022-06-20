

namespace TestHarness.Ext.Authentication.MSAL;

public partial class MsalAuthenticationHomeViewModel : ObservableObject
{
	public INavigator Navigator { get; init; }
	public IAuthenticationFlow Flow { get; init; }

	public IMsalAuthenticationTaskListEndpoint TaskEndpoint { get; init; }

	[ObservableProperty]
	private MsalAuthenticationToDoTaskListData[]? tasks;

	public MsalAuthenticationHomeViewModel(INavigator navigator, IAuthenticationFlow flow, IMsalAuthenticationTaskListEndpoint taskEndpoint)
	{
		Navigator = navigator;
		Flow = flow;
		TaskEndpoint = taskEndpoint;
	}

	public async Task Logout()
	{
		await Flow.LogoutAsync(CancellationToken.None);
	}

	public async Task RetrieveTasks()
	{
		var tasksResponse = await TaskEndpoint.GetAllAsync(CancellationToken.None);
		Tasks = tasksResponse?.Value?.ToArray();
	}
}
