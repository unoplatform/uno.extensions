using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Uno.Extensions.Http.Handlers
{
	/// <summary>
	/// This proxy ensures that only 1 RefreshToken operation runs a any time.
	/// It also ensures that only 1 NotifySessionExpired operation runs for equivalent expirations.
	/// </summary>
	/// <typeparam name="TAuthenticationToken">Type of authentication token</typeparam>
	public class ConcurrentAuthenticationTokenProvider<TAuthenticationToken> : IAuthenticationTokenProvider<TAuthenticationToken>
		where TAuthenticationToken : IAuthenticationToken
	{
		private readonly Func<CancellationToken, HttpRequestMessage, Task<TAuthenticationToken>> _getToken;
		private readonly Func<CancellationToken, HttpRequestMessage, TAuthenticationToken, Task> _notifySessionExpired;
		private readonly Func<CancellationToken, HttpRequestMessage, TAuthenticationToken, Task<TAuthenticationToken>> _refreshToken;

		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
		private readonly ILogger _logger;

		private TAuthenticationToken _lastUnauthorizedToken;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentAuthenticationTokenProvider{TAuthenticationToken}"/> class.
		/// </summary>
		/// <param name="loggerFactory">The logger factory to create the internal logger for this class.</param>
		/// <param name="getToken">Method to retrieve the <typeparamref name="TAuthenticationToken"/>.</param>
		/// <param name="notifySessionExpired">Method to call when the <typeparamref name="TAuthenticationToken"/> is considered expired.</param>
		/// <param name="refreshToken">Method to refresh the token (only called if the token can be refreshed)</param>
		public ConcurrentAuthenticationTokenProvider(
			ILoggerFactory loggerFactory,
			Func<CancellationToken, HttpRequestMessage, Task<TAuthenticationToken>> getToken,
			Func<CancellationToken, HttpRequestMessage, TAuthenticationToken, Task> notifySessionExpired,
			Func<CancellationToken, HttpRequestMessage, TAuthenticationToken, Task<TAuthenticationToken>> refreshToken = null
		)
		{
			_logger = loggerFactory?.CreateLogger<ConcurrentAuthenticationTokenProvider<TAuthenticationToken>>() ?? NullLogger<ConcurrentAuthenticationTokenProvider<TAuthenticationToken>>.Instance;
			_getToken = getToken ?? throw new ArgumentNullException(nameof(getToken));
			_notifySessionExpired = notifySessionExpired ?? throw new ArgumentNullException(nameof(notifySessionExpired));
			_refreshToken = refreshToken;
		}

		/// <inheritdoc />
		public Task<TAuthenticationToken> GetToken(CancellationToken ct, HttpRequestMessage request) =>
			_getToken.Invoke(ct, request);

		/// <inheritdoc />
		public async Task NotifySessionExpired(CancellationToken ct, HttpRequestMessage request, TAuthenticationToken unauthorizedToken)
		{
			// Avoid notifiying more than once for the same token expiration.
			if (_lastUnauthorizedToken?.AccessToken != unauthorizedToken?.AccessToken)
			{
				_lastUnauthorizedToken = unauthorizedToken;
				await _notifySessionExpired(ct, request, unauthorizedToken);
			}
		}

		/// <inheritdoc />
		public async Task<TAuthenticationToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TAuthenticationToken unauthorizedToken)
		{
			if (_refreshToken == null)
			{
				throw new NotSupportedException("This authentication token provider doesn't support refreshing the token.");
			}

			// Wait for other refresh operations to finish.
			await _semaphore.WaitAsync(ct);

			try
			{
				// From this moment, the operation cannot be cancelled.
				var refreshedToken = await GetRefreshedAuthenticationToken(CancellationToken.None);

				return refreshedToken;
			}
			finally
			{
				// Release the semaphore.
				_semaphore.Release();
			}

			async Task<TAuthenticationToken> GetRefreshedAuthenticationToken(CancellationToken ct2)
			{
				// We get the current authentication token inside the lock
				// as it's very possible that the unauthorized token is no
				// longer the current token because another refresh request was made.
				var currentToken = await GetToken(ct2, request);

				_logger.LogDebug($"The current token is: '{currentToken}'.");

				// If we don't have an authentication data or a refresh token, we cannot refresh the access token.
				// This can happen if the session has expired while 2 concurrent refresh requests were made.
				// The second request will not have a refresh token.
				if (currentToken == null || !currentToken.CanBeRefreshed)
				{
					_logger.LogWarning($"The refresh token is null or cannot be refreshed.");

					return default;
				}

				// If we have an access token but it's not the same, the token has been refreshed.
				if (currentToken.AccessToken != null &&
					currentToken.AccessToken != unauthorizedToken.AccessToken)
				{
					_logger.LogWarning($"The access tokens are different. No need to refresh, returning the current token '{currentToken}'.");

					return currentToken;
				}

				try
				{
					_logger.LogDebug($"Refreshing token: '{unauthorizedToken}'.");

					var refreshedToken = await _refreshToken(ct2, request, currentToken);

					_logger.LogInformation($"Refreshed token: '{unauthorizedToken}' to '{refreshedToken}'.");

					return refreshedToken;
				}
				catch (Exception e)
				{
					_logger.LogError(e, $"Failed to refresh token: '{unauthorizedToken}'.");

					return default;
				}
			}
		}
	}
}
