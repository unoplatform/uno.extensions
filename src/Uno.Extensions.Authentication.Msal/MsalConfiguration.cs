namespace Uno.Extensions.Authentication.MSAL;

internal class MsalConfiguration : PublicClientApplicationOptions
{
	public string[]? Scopes { get; init; }
}
