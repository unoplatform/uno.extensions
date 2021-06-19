using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http
{
    public interface IAuthenticationEndpoint
    {
        /// <summary>
        /// Logs the user in using the provided <paramref name="email"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="email">Email</param>
        /// <param name="password">Password</param>
        /// <returns><see cref="AuthenticationData"/></returns>
        Task<AuthenticationData> Login(CancellationToken ct, string email, string password);

        /// <summary>
        /// Refreshes the user token.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="unauthorizedToken">Unauthorized token</param>
        /// <returns><see cref="AuthenticationData"/></returns>
        Task<AuthenticationData> RefreshToken(CancellationToken ct, AuthenticationData unauthorizedToken);

        /// <summary>
        /// Creates a user account.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="email">Email</param>
        /// <param name="password">Password</param>
        /// <returns><see cref="Task"/></returns>
        Task CreateAccount(CancellationToken ct, string email, string password);

        /// <summary>
        /// Resets the password.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="email">Email</param>
        /// <returns><see cref="Task"/></returns>
        Task ResetPassword(CancellationToken ct, string email);
    }
}
