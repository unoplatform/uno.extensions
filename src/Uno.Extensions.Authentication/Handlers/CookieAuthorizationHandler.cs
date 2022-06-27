namespace Uno.Extensions.Authentication.Handlers;

internal class CookieAuthorizationHandler : BaseAuthorizationHandler
{
	public CookieAuthorizationHandler(
		ILogger<BaseAuthorizationHandler> logger,
		IAuthenticationService authenticationService,
		ITokenCache tokens,
		HandlerSettings settings
	) : base(logger, authenticationService,
					 tokens, settings)
	{
	}

	public override bool ShouldIncludeToken(HttpRequestMessage request) => true;
	protected override async Task<bool> ApplyTokensToRequest(HttpRequestMessage request, IDictionary<string, string> tokens, CancellationToken ct)
	{
		var cookies = (this.InnerHandler as HttpClientHandler)?.CookieContainer;
		if (cookies is not null && request.RequestUri is not null)
		{
			var accessToken = await _tokens.AccessTokenAsync();
			if (!string.IsNullOrWhiteSpace(accessToken) &&
				!string.IsNullOrWhiteSpace(_settings.CookieAccessToken))
			{
				cookies.Add(request.RequestUri, new Cookie(_settings.CookieAccessToken, accessToken));
			}
			var refreshToken = await _tokens.RefreshTokenAsync();
			if (!string.IsNullOrWhiteSpace(refreshToken) &&
				!string.IsNullOrWhiteSpace(_settings.CookieRefreshToken))
			{
				cookies.Add(request.RequestUri, new Cookie(_settings.CookieRefreshToken, refreshToken));
			}
			return true;
		}
		return false;
	}

	protected override async Task<bool> ExtractTokensFromResponse(
			HttpRequestMessage request,
			IDictionary<string, string> tokens,
			CancellationToken ct)
	{
		var cookies = (this.InnerHandler as HttpClientHandler)?.CookieContainer;
		if (cookies is not null && request.RequestUri is not null)
		{
			var access = !string.IsNullOrWhiteSpace(_settings.CookieAccessToken) ? cookies.GetCookies(request.RequestUri)[_settings.CookieAccessToken]?.Value : default;
			var refresh = !string.IsNullOrWhiteSpace(_settings.CookieRefreshToken) ? cookies.GetCookies(request.RequestUri)[_settings.CookieRefreshToken]?.Value : default;
			await _tokens.SaveTokensAsync(access, refresh);
			return true;
		}
		return false;
	}

}
