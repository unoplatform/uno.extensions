namespace Uno.Extensions.Authentication.MSAL;

internal record MsalConfiguration
{
	public string? ClientId { get; init; }
	public string[]? Scopes { get; init; }
	public string? RedirectUri { get; init; }
	public string? KeychainSecurityGroup { get; init; }
}
