namespace Uno.Extensions.Authentication.Web;

public record WebConfiguration
{
	public bool PrefersEphemeralWebBrowserSession { get; init; }
	public string? LoginStartUri { get; init; }
	public string? LoginCallbackUri { get; init; }
	public string? TokenEndpoint { get; init; }
	public string? UserInfoEndpoint { get; init; }
	public bool UsePkce { get; init; } = false;
	public string? AccessTokenKey { get; init; }
	public string? RefreshTokenKey { get; init; }
	public string? IdTokenKey { get; init; }
	public IDictionary<string, string>? OtherTokenKeys { get; init; }
	public string? LogoutStartUri { get; init; }
	public string? LogoutCallbackUri { get; init; }
}
