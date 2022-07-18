namespace Uno.Extensions.Authentication.Web;

internal record WebAuthenticationSettings
{
	public string? LoginStartUri { get; init; }

	public AsyncFunc<string?, IDictionary<string, string>?, string>? PrepareLoginStartUri { get; init; }

	public string? LoginCallbackUri { get; init; }

	public AsyncFunc<string?, IDictionary<string, string>?, string>? PrepareLoginCallbackUri { get; init; }

	public string? AccessTokenKey { get; init; } = "access_token";
	public string? RefreshTokenKey { get; init; } = "refresh_token";

	public IDictionary<string,string>? OtherTokenKeys { get; init; }

	public string? LogoutStartUri { get; init; }

	public AsyncFunc<string?, IDictionary<string, string>?, string>? PrepareLogoutStartUri { get; init; }

	public string? LogoutCallbackUri { get; set; }

	public AsyncFunc<string?, IDictionary<string, string>?, string>? PrepareLogoutCallbackUri { get; init; }

	public AsyncFunc<IServiceProvider, IDictionary<string, string>, IDictionary<string, string>?>? RefreshCallback { get; init; }
}

internal record WebAuthenticationSettings<TService>: WebAuthenticationSettings
	where TService: notnull
{
	public new AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?>? RefreshCallback;
}
