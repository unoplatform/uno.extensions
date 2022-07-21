
namespace TestHarness.Ext.Authentication.Custom;

public record CustomAuthenticationShellViewModel
{
	public CustomAuthenticationShellViewModel(IAuthenticationFlow flow, INavigator navigator, IDispatcher dispatcher)
	{
		flow.Initialize(dispatcher, navigator);

		_ = flow.AuthenticatedNavigateRouteAsync(this, "Home");
	}
}
