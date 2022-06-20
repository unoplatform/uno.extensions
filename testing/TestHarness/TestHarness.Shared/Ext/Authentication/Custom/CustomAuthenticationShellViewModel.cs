
namespace TestHarness.Ext.Authentication.Custom;

public record CustomAuthenticationShellViewModel
{
	private readonly INavigator _navigator;
	private readonly IAuthenticationFlow _flow;
	public CustomAuthenticationShellViewModel(IAuthenticationFlow flow, INavigator navigator, IDispatcher dispatcher)
	{
		_navigator=navigator;
		_flow = flow;

		flow.Initialize(dispatcher, navigator);

		_ = Launch();
	}


	private async Task Launch()
	{
		var authenticated = await _flow.EnsureAuthenticatedAsync(CancellationToken.None);
		if(authenticated)
		{
			await _navigator.NavigateViewModelAsync<CustomAuthenticationHomeViewModel>(this);
		}
	}
}
