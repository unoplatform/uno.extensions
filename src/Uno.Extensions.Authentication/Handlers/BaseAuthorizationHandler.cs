

using Uno.Extensions.Logging;

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


	/// <summary>
	/// This is intentionally a static so that all instances of the BaseAuthorizationHandler
	/// access the same semaphore to block concurrent access to the refresh token logic
	/// </summary>
	private static SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1);

	/// <inheritdoc/>
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
	{
		var noRefresh = request.Headers.Any(x =>
							x.Key == Headers.NoRefreshKey &&
							x.Value.Any(v => bool.TryParse(v, out bool boolV) ? boolV : false));
		if (noRefresh)
		{
			request.Headers.Remove(Headers.NoRefreshKey);
		}

		if (!ShouldIncludeToken(request))
		{
			// Request doesn't require an authentication token.
			if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"The request '{request.RequestUri}' doesn't require an authentication token.");

			return await base.SendAsync(request, ct);
		}

		var currentTokens = await _tokens.GetAsync(ct);
		var response = await SendWithAuthenticationToken(request, ct);

		if (!IsUnauthorized(request, response))
		{
			if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage("Request isn't unauthorized");
			if (response.IsSuccessStatusCode)
			{
				if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage("Request successful, extracting tokens");

				await ExtractTokensFromResponse(request, response, ct);
			}

			// Request was authorized, return the response.
			return response;
		}

		if (noRefresh)
		{
			if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"The request '{request.RequestUri}' has been excluded from refresh token logic, so clearing token cache.");
			// Request was unauthorized and we cannot refresh the authentication token.
			await _tokens.ClearAsync(ct);
			return response;
		}

		if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"Authentication needs to refresh, so grab access to the refresh semaphore. WARNING: If {request.RequestUri} is an endpoint for refreshing tokens, make sure it's annotated with Header attribute {Headers.NoRefresh} to avoid recursion");
		await _refreshSemaphore.WaitAsync();
		try
		{
			// Once we have the refresh semaphore, we should check to see if the tokens have changed
			// If they've changed, then likely another refresh operation took place, so we should
			// skip refresh and just try the service call again
			var refreshedTokens = await _tokens.GetAsync(ct);

			// Check the token dictionaries
			var match = currentTokens.Count == refreshedTokens.Count && !currentTokens.Except(refreshedTokens).Any();
			if (match)
			{
				if (!await _authenticationService.IsAuthenticated())
				{
					if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"The request '{request.RequestUri}' was unauthorized and the tokens cannot be refreshed. Considering the session has expired.");

			// Request was unauthorized and we cannot refresh the authentication token.
			await _tokens.ClearAsync(ct);

					return response;
				}

				var refreshedToken = await RefreshAuthenticationToken(ct);

				if (!refreshedToken)
				{
					if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"The request '{request.RequestUri}' was unauthorized and the token failed to refresh. Considering the session has expired.");

			// No authentication token to use.
			await _tokens.ClearAsync(ct);

					return response;
				}
			}
		}
		finally
		{
			_refreshSemaphore.Release();
		}

		response = await SendWithAuthenticationToken(request, ct);

		if (IsUnauthorized(request, response))
		{
			if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"The request '{request.RequestUri}' was unauthorized, the tokens were refreshed to but the request was still unauthorized. Considering the session has expired.");

			// Request was still unauthorized and we cannot refresh the authentication token.
			await _tokens.ClearAsync(ct);

			return response;
		}

		await ExtractTokensFromResponse(request, response, ct);
		return response;

	}

	private async Task<HttpResponseMessage> SendWithAuthenticationToken(
		HttpRequestMessage request,
		CancellationToken ct)
	{
		await ApplyTokensToRequest(request, ct);

		return await base.SendAsync(request, ct);
	}

	protected abstract Task<bool> ApplyTokensToRequest(
	HttpRequestMessage request,
	CancellationToken ct);

	protected virtual Task<bool> ExtractTokensFromResponse(
	HttpRequestMessage request,
	HttpResponseMessage response,
	CancellationToken ct) => Task.FromResult(true);


	protected virtual async Task<bool> RefreshAuthenticationToken(CancellationToken ct)
	{
		if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"Requesting refresh for token.");
		try
		{
			return await _authenticationService.RefreshAsync(ct);
		}
		catch(Exception ex)
		{
			if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebugMessage($"Error refreshing token - {ex.Message}");
			return false;
		}
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
