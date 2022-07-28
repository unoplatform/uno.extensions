
namespace TestHarness.Ext.Authentication.Web;

public record WebAuthenticationShellViewModel
{
	private readonly INavigator _navigator;
	private readonly IAuthenticationFlow _flow;
	public WebAuthenticationShellViewModel(IAuthenticationFlow flow, INavigator navigator, IDispatcher dispatcher)
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
			await _navigator.NavigateViewModelAsync<WebAuthenticationHomeViewModel>(this);
		}
	}
}
