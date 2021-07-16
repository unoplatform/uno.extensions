using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
    /// <summary>
    /// An <see cref="IAuthenticationTokenProvider{TAuthenticationToken}"/> owns the authentication token.
    /// Typically that would be your application's AuthenticationService.
    /// We strongly suggest you use <see cref="ConcurrentAuthenticationTokenProvider{TAuthenticationToken}"/> so that you don't need to deal with concurrency handling in your application code.
    /// </summary>
    /// <typeparam name="TAuthenticationToken">The type of authentication token.</typeparam>
    public interface IAuthenticationTokenProvider<TAuthenticationToken>
        where TAuthenticationToken : IAuthenticationToken
    {
        /// <summary>
        /// Returns the current authentication token that would be used
        /// for the specified <paramref name="request"/>.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/>.</param>
        /// <param name="request"><see cref="HttpRequestMessage"/>.</param>
        /// <returns><typeparamref name="TAuthenticationToken"/>.</returns>
        Task<TAuthenticationToken> GetToken(CancellationToken ct, HttpRequestMessage request);

        /// <summary>
        /// Refreshes the authentication token.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/>.</param>
        /// <param name="request"><see cref="HttpRequestMessage"/>.</param>
        /// <param name="unauthorizedToken"><typeparamref name="TAuthenticationToken"/>.</param>
        /// <returns><typeparamref name="TAuthenticationToken"/>.</returns>
        Task<TAuthenticationToken> RefreshToken(CancellationToken ct, HttpRequestMessage request, TAuthenticationToken unauthorizedToken);

        /// <summary>
        /// Occurs when then session has expired, meaning that the authentication token cannot be used anymore.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/>.</param>
        /// <param name="request"><see cref="HttpRequestMessage"/>.</param>
        /// <param name="unauthorizedToken"><typeparamref name="TAuthenticationToken"/>.</param>
        /// <returns><see cref="Task"/>.</returns>
        Task NotifySessionExpired(CancellationToken ct, HttpRequestMessage request, TAuthenticationToken unauthorizedToken);
    }
}
