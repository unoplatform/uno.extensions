using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
	/// <summary>
	/// This is a simple proxy for <see cref="IAuthenticationTokenProvider{TAuthenticationToken}"/>.
	/// It helps break the circular dependency that might originate when implementing the interface.
	/// - Service A uses Endpoint B.
	/// - Endpoint B uses AuthenticationTokenHandler.
	/// - AuthenticationTokenHandler uses IAuthenticationTokenProvider.
	/// - IAuthenticationTokenProvider uses Service A.
	/// </summary>
	/// <typeparam name="TAuthenticationToken">Type of authentication token</typeparam>
	[Obsolete("AuthenticationTokenProvider is obsolete. Use ConcurrentAuthenticationTokenProvider instead.", error: true)]
	public class AuthenticationTokenProvider<TAuthenticationToken> : IAuthenticationTokenProvider<TAuthenticationToken>
		where TAuthenticationToken : IAuthenticationToken
	{
		private readonly Func<CancellationToken, HttpRequestMessage, Task<TAuthenticationToken>> _getToken;
		private readonly Func<CancellationToken, HttpRequestMessage, TAuthenticationToken, Task> _notifySessionExpired;
		private readonly Func<CancellationToken, HttpRequestMessage, TAuthenticationToken, Task<TAuthenticationToken>> _refreshToken;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticationTokenProvider{TAuthenticationToken}"/> class.
		/// </summary>
		/// <param name="getToken">Method to retrieve the <typeparamref name="TAuthenticationToken"/>.</param>
		/// <param name="notifySessionExpired">Method to call when the <typeparamref name="TAuthenticationToken"/> is considered expired.</param>
		/// <param name="refreshToken">Method to refresh the token (only called if the token can be refreshed)</param>
		[Obsolete("AuthenticationTokenProvider is obsolete. Use ConcurrentAuthenticationTokenProvider instead.", error: true)]
		public AuthenticationTokenProvider(
			Func<CancellationToken, HttpRequestMessage, Task<TAuthenticationToken>> getToken,
			Func<CancellationToken, HttpRequestMessage, TAuthenticationToken, Task> notifySessionExpired,
			Func<CancellationToken, HttpRequestMessage, TAuthenticationToken, Task<TAuthenticationToken>> refreshToken = null
		)
		{
			_getToken = getToken ?? throw new ArgumentNullException(nameof(getToken));
			_notifySessionExpired = notifySessionExpired ?? throw new ArgumentNullException(nameof(notifySessionExpired));
			_refreshToken = refreshToken;
		}

		/// <inheritdoc />
		public Task<TAuthenticationToken> GetToken(CancellationToken ct, HttpRequestMessage request) =>
			_getToken.Invoke(ct, request);

		/// <inheritdoc />
		public Task NotifySessionExpired(CancellationToken ct, HttpRequestMessage request, TAuthenticationToken unauthorizedToken) =>
			_notifySessionExpired.Invoke(ct, request, unauthorizedToken);

		/// <inheritdoc />
		public async Task<TAuthenticationToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TAuthenticationToken unauthorizedToken)
		{
			if (_refreshToken == null)
			{
				throw new NotSupportedException("This authentication token provider doesn't support refreshing the token.");
			}

			return await _refreshToken.Invoke(ct, request, unauthorizedToken);
		}
	}
}
