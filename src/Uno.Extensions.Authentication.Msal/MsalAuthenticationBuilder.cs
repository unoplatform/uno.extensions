namespace Uno.Extensions.Authentication.MSAL;

internal record MsalAuthenticationBuilder : BaseBuilder<MsalAuthenticationSettings>, IMsalAuthenticationBuilder
{
	public PublicClientApplicationBuilder? MsalBuilder { get; set; }
}
