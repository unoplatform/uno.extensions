namespace Uno.Extensions.Authentication.Handlers;

internal class CookieHandler : BaseAuthorizationHandler
{
	// This is any string since it doesn't correspond to any particular provider
	// During authentication if cookies are retrieved, they'll be saved to the
	// token cache using either the current provider (eg refreshing tokens) or
	// using the TemporaryProviderKey. As part of completing the login process,
	// The AuthenticationService will correct the current provider in the token cache
	private const string TemporaryProviderKey = "Cookie";
	public CookieHandler(
		ILogger<BaseAuthorizationHandler> logger,
		IAuthenticationService authenticationService,
		ITokenCache tokens,
		HandlerSettings settings
	) : base(logger, authenticationService,
					 tokens, settings)
	{
	}

	public override bool ShouldIncludeToken(HttpRequestMessage request) => true;
	protected override async Task<bool> ApplyTokensToRequest(HttpRequestMessage request, CancellationToken ct)
	{
		var accessToken = await _tokens.AccessTokenAsync();
		var refreshToken = await _tokens.RefreshTokenAsync();

		// Return false if we don't have either access or refresh token
		if (
			(string.IsNullOrWhiteSpace(accessToken) ||
			string.IsNullOrWhiteSpace(_settings.CookieAccessToken))
			&&
			(string.IsNullOrWhiteSpace(refreshToken) ||
			string.IsNullOrWhiteSpace(_settings.CookieRefreshToken))
			)
		{
			return false;
		}

		var cookies = new CookieContainer();

		if (!string.IsNullOrWhiteSpace(accessToken) &&
			!string.IsNullOrWhiteSpace(_settings.CookieAccessToken))
		{
			cookies.Add(request.RequestUri, new Cookie(_settings.CookieAccessToken, accessToken));
		}

		if (!string.IsNullOrWhiteSpace(refreshToken) &&
			!string.IsNullOrWhiteSpace(_settings.CookieRefreshToken))
		{
			cookies.Add(request.RequestUri, new Cookie(_settings.CookieRefreshToken, refreshToken));
		}

		request.Headers.Add("Cookie", cookies.GetCookieHeader(request.RequestUri));

		return true;
	}

	protected override async Task<bool> ExtractTokensFromResponse(
			HttpRequestMessage request,
			HttpResponseMessage response,
			CancellationToken ct)
	{

		var cookies = new CookieContainer();
		var cookieHeader = response.Headers.FirstOrDefault(x => x.Key == "Set-Cookie").Value;
		if (cookieHeader?.Any() ?? false)
		{
			cookies.SetCookies(request.RequestUri, string.Join(",", cookieHeader));
		}

		var access = !string.IsNullOrWhiteSpace(_settings.CookieAccessToken) ? cookies.GetCookies(request.RequestUri)[_settings.CookieAccessToken]?.Value : default;
		access = access ?? await _tokens.AccessTokenAsync();
		var refresh = !string.IsNullOrWhiteSpace(_settings.CookieRefreshToken) ? cookies.GetCookies(request.RequestUri)[_settings.CookieRefreshToken]?.Value : default;
		refresh = refresh ?? await _tokens.RefreshTokenAsync();

		await _tokens.SaveTokensAsync(_tokens.CurrentProvider ?? TemporaryProviderKey, access, refresh);
		return true;

	}

}
