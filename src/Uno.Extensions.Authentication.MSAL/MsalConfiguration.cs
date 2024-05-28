namespace Uno.Extensions.Authentication.MSAL;

internal class MsalConfiguration
#if UNO_EXT_MSAL
	: PublicClientApplicationOptions
#endif
{
	public string[]? Scopes { get; init; }
}
