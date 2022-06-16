namespace Uno.Extensions.Authentication.MSAL;

public interface IMsalAuthenticationBuilder : IBuilder
{
	PublicClientApplicationBuilder? MsalBuilder { get; set; }
}
