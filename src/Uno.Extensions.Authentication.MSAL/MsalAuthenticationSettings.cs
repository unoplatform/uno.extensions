

namespace Uno.Extensions.Authentication.MSAL;

internal record MsalAuthenticationSettings
{
#if UNO_EXT_MSAL
	public Action<PublicClientApplicationBuilder>? Build { get; init; }

	public Action<StorageCreationPropertiesBuilder>? Store { get; init; }
#endif
	public string[]? Scopes { get; init; }
}
