namespace Uno.Extensions.Authentication.Handlers;

internal record HandlerSettings
{
	public bool NoHandlers { get; init; }
	public string? CookieAccessToken { get; init; }
	public string? CookieRefreshToken { get; init; }
	public string? AuthorizationHeaderScheme { get; init; } = "Bearer";
}
