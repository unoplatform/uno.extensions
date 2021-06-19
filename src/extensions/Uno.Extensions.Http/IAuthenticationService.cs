using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Gets and observes the current <see cref="AuthenticationData"/>.
        /// </summary>
        /// <returns>Current <see cref="AuthenticationData"/></returns>
        AuthenticationData AuthenticationData { get; }

        /// <summary>
        /// Gets a boolean indicating whether or not the user is currently authenticated.
        /// </summary>
        /// <returns>Whether or not the user is currently authenticated.</returns>
        bool IsAuthenticated { get; }

        IObservable<Unit> ObserveSessionExpired();

        /// <summary>
        /// Logs the user in using the provided <paramref name="email"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="email">Email</param>
        /// <param name="password">Password</param>
        /// <returns><see cref="AuthenticationData"/></returns>
        Task<AuthenticationData> Login(CancellationToken ct, string email, string password);

        /// <summary>
        /// Logs the user out.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <returns><see cref="Task"/></returns>
        Task Logout(CancellationToken ct);

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
