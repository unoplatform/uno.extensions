


namespace Uno.Extensions.Authentication.Oidc;

internal record OidcAuthenticationSettings
{
	public OidcClientOptions? Options { get; init; }

	public bool AutoRedirectUri { get; init; } = PlatformHelper.IsWebAssembly;
}
