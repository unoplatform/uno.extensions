namespace Uno.Extensions.Authentication.Web;

internal record WebConfiguration
{
	public bool PrefersEphemeralWebBrowserSession { get; init; }
	public string? LoginStartUri { get; init; }
	public string? LoginCallbackUri { get; init; }
	public TokenCacheOptions? TokenCacheOptions { get; init; }
	public UriTokenOptions? UriTokenOptions { get; init; }
	public string? LogoutStartUri { get; init; }
	public string? LogoutCallbackUri { get; init; }
}
