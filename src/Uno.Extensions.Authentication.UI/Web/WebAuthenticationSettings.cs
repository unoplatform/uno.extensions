namespace Uno.Extensions.Authentication.Web;

internal record WebAuthenticationSettings
{
	public bool PrefersEphemeralWebBrowserSession { get; init; }

	public string? LoginStartUri { get; init; }

	public AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string>? PrepareLoginStartUri { get; init; }

	public string? LoginCallbackUri { get; init; }

	public AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string>? PrepareLoginCallbackUri { get; init; }

	public string AccessTokenKey { get; init; } = "access_token";
	public string RefreshTokenKey { get; init; } = "refresh_token";

	public AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string, IDictionary<string, string>, IDictionary<string, string>?>? PostLoginCallback { get; init; }

	public string? LogoutStartUri { get; init; }

	public AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string>? PrepareLogoutStartUri { get; init; }

	public string? LogoutCallbackUri { get; set; }

	public AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string>? PrepareLogoutCallbackUri { get; init; }

	public AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?>? RefreshCallback { get; init; }
}

internal record WebAuthenticationSettings<TService> : WebAuthenticationSettings
	where TService : notnull
{
	public new AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string>? PrepareLoginStartUri { get; init; }

	public new AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string>? PrepareLoginCallbackUri { get; init; }

	public new AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string, IDictionary<string, string>, IDictionary<string, string>?>? PostLoginCallback { get; init; }

	public new AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string>? PrepareLogoutStartUri { get; init; }

	public new AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string>? PrepareLogoutCallbackUri { get; init; }

	public new AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?>? RefreshCallback;
}
