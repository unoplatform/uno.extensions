

namespace Uno.Extensions.Authentication.MSAL;

internal record MsalAuthenticationSettings
{
	public Action<PublicClientApplicationBuilder>? Build { get; init; }

	public Action<StorageCreationPropertiesBuilder>? Store { get; init; }

	public string[]? Scopes { get; init; }
}
