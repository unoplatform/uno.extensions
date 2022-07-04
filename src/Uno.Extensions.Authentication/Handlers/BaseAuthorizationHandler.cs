

namespace Uno.Extensions.Authentication.Handlers;

internal abstract class BaseAuthorizationHandler : DelegatingHandler
{
	protected readonly ITokenCache _tokens;
	protected readonly ILogger _logger;
	protected readonly IAuthenticationService _authenticationService;
	protected HandlerSettings _settings;

	protected BaseAuthorizationHandler(
		ILogger<BaseAuthorizationHandler> logger,
		IAuthenticationService authenticationService,
		ITokenCache tokens,
		HandlerSettings settings
	)
	{
		_logger = logger;
		_tokens = tokens;
		_authenticationService = authenticationService;
		_settings = settings;
	}

	/// <inheritdoc/>
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
	{
		if (!ShouldIncludeToken(request))
		{
			// Request doesn't require an authentication token.
			_logger.LogDebug($"The request '{request.RequestUri}' doesn't require an authentication token.");

			return await base.SendAsync(request, ct);
		}

		var tokens = await _tokens.GetAsync();

		var response = await SendWithAuthenticationToken(request, tokens, ct);

		if (!IsUnauthorized(request, response))
		{
			// Request was authorized, return the response.
			return response;
		}

		if (!await _authenticationService.CanRefresh())
		{
			_logger.LogError($"The request '{request.RequestUri}' was unauthorized and the tokens cannot be refreshed. Considering the session has expired.");

			// Request was unauthorized and we cannot refresh the authentication token.
			await _tokens.ClearAsync();

			return response;
		}

		var refreshedToken = await RefreshAuthenticationToken(ct);

		if (!refreshedToken)
		{
			_logger.LogError($"The request '{request.RequestUri}' was unauthorized and the token failed to refresh. Considering the session has expired.");

			// No authentication token to use.
			await _tokens.ClearAsync();

			return response;
		}

		response = await SendWithAuthenticationToken(request, tokens, ct);

		if (IsUnauthorized(request, response))
		{
			_logger.LogError($"The request '{request.RequestUri}' was unauthorized, the tokens were refreshed to but the request was still unauthorized. Considering the session has expired.");

			// Request was still unauthorized and we cannot refresh the authentication token.
			await _tokens.ClearAsync();

			return response;
		}

		return response;
	}

	protected virtual async Task<HttpResponseMessage> SendWithAuthenticationToken(

		HttpRequestMessage request,
		IDictionary<string, string> tokens,
				CancellationToken ct
	)
	{
		await ApplyTokensToRequest(request, tokens, ct);

		return await base.SendAsync(request, ct);
	}

	protected abstract Task<bool> ApplyTokensToRequest(
	HttpRequestMessage request,
	IDictionary<string, string> tokens,
	CancellationToken ct);

	protected virtual Task<bool> ExtractTokensFromResponse(
	HttpRequestMessage request,
	IDictionary<string, string> tokens,
	CancellationToken ct) => Task.FromResult(true);


	protected virtual async Task<bool> RefreshAuthenticationToken(CancellationToken ct)
	{
		_logger.LogDebug($"Requesting refresh for token.");

		return await _authenticationService.RefreshAsync(ct);
	}

	/// <summary>
	/// Returns whether or not the handler should include an authentication token.
	/// </summary>
	/// <param name="request"><see cref="HttpRequestMessage"/></param>
	/// <returns>Whether or not the request should include an authentication token</returns>
	public virtual bool ShouldIncludeToken(HttpRequestMessage request) => false;

	public virtual bool IsUnauthorized(HttpRequestMessage request, HttpResponseMessage response)
		=> response.StatusCode == HttpStatusCode.Unauthorized;
}
