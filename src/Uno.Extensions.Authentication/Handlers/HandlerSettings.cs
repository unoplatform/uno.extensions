namespace Uno.Extensions.Authentication.Handlers;

internal record HandlerSettings
{
	public string? CookieAccessToken { get; init; }
	public string? CookieRefreshToken { get; init; }
	public string? AuthorizationHeaderScheme { get; init; }
}
