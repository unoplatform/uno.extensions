
namespace TestHarness.Ext.Authentication.MSAL;

public record MsalAuthenticationShellViewModel
{
	public MsalAuthenticationShellViewModel(IAuthenticationFlow flow, INavigator navigator, IDispatcher dispatcher)
	{

		flow.Initialize(dispatcher, navigator);

		_ = flow.EnsureAuthenticatedAsync();
	}

}
