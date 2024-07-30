namespace Uno.Extensions.Authentication.Handlers;

internal class CookieHandler : BaseAuthorizationHandler
{
	// This is any string since it doesn't correspond to any particular provider
	// During authentication if cookies are retrieved, they'll be saved to the
	// token cache using either the current provider (eg refreshing tokens) or
	// using the TemporaryProviderKey. As part of completing the login process,
	// The AuthenticationService will correct the current provider in the token cache
	private const string TemporaryProviderKey = "Cookie";

	private ICookieManager _cookieManager;

	public CookieHandler(
		ILogger<CookieHandler> logger,
		IAuthenticationService authenticationService,
		ITokenCache tokens,
		HandlerSettings settings,
		ICookieManager cookieManager
	) : base(logger, authenticationService,
					 tokens, settings)
	{
		_cookieManager = cookieManager;
	}

	public override bool ShouldIncludeToken(HttpRequestMessage request) => true;
	protected override async Task<bool> ApplyTokensToRequest(HttpRequestMessage request, CancellationToken ct)
	{
		var accessToken = await _tokens.AccessTokenAsync() ?? string.Empty;
		var refreshToken = await _tokens.RefreshTokenAsync() ?? string.Empty;

		if (request.RequestUri is null)
		{
			return false;
		}

		var builder = new UriBuilder(request.RequestUri);
		builder.Path = string.Empty;
		var baseUrl = builder.Uri;

		// Forcibly expire any existing cookie
		var cookies = this.InnerHandler is { } innerHandler ? _cookieManager.ClearCookies(innerHandler, baseUrl) : default;
		var setHeaders = cookies == null;
		cookies ??= new CookieContainer();

		// Return false if we don't have either access or refresh token
		if (
			(
			//string.IsNullOrWhiteSpace(accessToken) ||
			string.IsNullOrWhiteSpace(_settings.CookieAccessToken))
			&&
			(
			//string.IsNullOrWhiteSpace(refreshToken) ||
			string.IsNullOrWhiteSpace(_settings.CookieRefreshToken))
			)
		{
			if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage("No access or refresh tokens to apply");
			return false;
		}

		if (
			//!string.IsNullOrWhiteSpace(accessToken) &&
			!string.IsNullOrWhiteSpace(_settings.CookieAccessToken))
		{
			cookies.Add(baseUrl, new Cookie(_settings.CookieAccessToken, accessToken));
		}

		if (
			//!string.IsNullOrWhiteSpace(refreshToken) &&
			!string.IsNullOrWhiteSpace(_settings.CookieRefreshToken))
		{
			cookies.Add(baseUrl, new Cookie(_settings.CookieRefreshToken, refreshToken));
		}

		if (setHeaders)
		{
			var headerString = cookies.GetCookieHeader(baseUrl);
			if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"Cookie: {headerString}");
			request.Headers.Add("Cookie", headerString);
		}

		return true;
	}

	protected override async Task<bool> ExtractTokensFromResponse(
			HttpRequestMessage request,
			HttpResponseMessage response,
			CancellationToken ct)
	{
		if (request.RequestUri is null)
		{
			return false;
		}

		var cookies = new CookieContainer();
		var cookieHeader = response.Headers.FirstOrDefault(x => x.Key == "Set-Cookie").Value;
		if (cookieHeader?.Any() ?? false)
		{
			var headerString = string.Join(",", cookieHeader);
			if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"Received Cookie: {headerString}");
			cookies.SetCookies(request.RequestUri, headerString);
		}

		var access = !string.IsNullOrWhiteSpace(_settings.CookieAccessToken) ? cookies.GetCookies(request.RequestUri)[_settings.CookieAccessToken]?.Value : default;
		access = access ?? await _tokens.AccessTokenAsync();
		if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"Access token: {access}");
		var refresh = !string.IsNullOrWhiteSpace(_settings.CookieRefreshToken) ? cookies.GetCookies(request.RequestUri)[_settings.CookieRefreshToken]?.Value : default;
		refresh = refresh ?? await _tokens.RefreshTokenAsync();
		if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"Refresh token: {refresh}");

		await _tokens.SaveTokensAsync((await _tokens.GetCurrentProviderAsync(ct)) ?? TemporaryProviderKey, access, refresh);
		return true;

	}

}
