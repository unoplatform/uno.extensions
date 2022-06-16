namespace Uno.Extensions.Authentication.Handlers;

internal class HeaderAuthorizationHandler : BaseAuthorizationHandler
{
	public HeaderAuthorizationHandler(
		ILogger<BaseAuthorizationHandler> logger,
		IAuthenticationService authenticationService,
		ITokenCache tokens,
		HandlerSettings settings
	) : base(logger, authenticationService, tokens, settings)
	{
	}
	protected override async Task<bool> ApplyTokensToRequest(HttpRequestMessage request, IDictionary<string, string> tokens, CancellationToken ct)
	{
		var accessToken = await _tokens.AccessTokenAsync();
		if (!string.IsNullOrWhiteSpace(accessToken) &&
			!string.IsNullOrWhiteSpace(_settings.AuthorizationHeaderScheme))
		{
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_settings.AuthorizationHeaderScheme, accessToken);
			return true;
		}

		return false;
	}
}
