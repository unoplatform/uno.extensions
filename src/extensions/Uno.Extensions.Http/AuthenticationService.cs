using System;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Configuration;
using Uno.Extensions.Http.Handlers;

namespace Uno.Extensions.Http
{
    public partial class AuthenticationService : IAuthenticationService, IAuthenticationTokenProvider<AuthenticationData>
    {
        private readonly ISubject<Unit> _sessionExpired = new Subject<Unit>();
        private readonly IWritableOptions<AuthenticationData> _authenticationData;
        private readonly IAuthenticationEndpoint _authenticationEndpoint;

        public AuthenticationService(
            IWritableOptions<AuthenticationData> authenticationData,
            IAuthenticationEndpoint authenticationEndpoint)
        {
            _authenticationData = authenticationData ?? throw new ArgumentNullException(nameof(authenticationData));
            _authenticationEndpoint = authenticationEndpoint ?? throw new ArgumentNullException(nameof(authenticationEndpoint));
        }

        /// <inheritdoc/>
        public AuthenticationData AuthenticationData => _authenticationData.Value;

        /// <inheritdoc/>
        public bool IsAuthenticated => AuthenticationData != default(AuthenticationData);

        /// <inheritdoc/>
        public IObservable<Unit> ObserveSessionExpired() => _sessionExpired;

        /// <inheritdoc/>
        public async Task<AuthenticationData> Login(CancellationToken ct, string email, string password)
        {
            var authenticationData = await _authenticationEndpoint.Login(ct, email, password);
            await _authenticationData.Update(auth => authenticationData);

            return authenticationData;
        }

        /// <inheritdoc/>
        public async Task Logout(CancellationToken ct)
        {
            await _authenticationData.Update(auth => null);
        }

        /// <inheritdoc/>
        public Task<AuthenticationData> GetToken(CancellationToken ct, HttpRequestMessage request)
        {
            var settings = _authenticationData.Value;

            return Task.FromResult(settings);
        }

        /// <inheritdoc/>
        public async Task<AuthenticationData> RefreshToken(CancellationToken ct, HttpRequestMessage request, AuthenticationData unauthorizedToken)
        {
            var authenticationData = await _authenticationEndpoint.RefreshToken(ct, unauthorizedToken);

            await _authenticationData.Update(auth => authenticationData);

            return authenticationData;
        }

        /// <inheritdoc/>
        public async Task NotifySessionExpired(CancellationToken ct, HttpRequestMessage request, AuthenticationData unauthorizedToken)
        {
            await _authenticationData.Update(auth => null);

            _sessionExpired.OnNext(Unit.Default);
        }

        /// <inheritdoc/>
        public async Task CreateAccount(CancellationToken ct, string email, string password)
        {
            await _authenticationEndpoint.CreateAccount(ct, email, password);
        }

        /// <inheritdoc/>
        public async Task ResetPassword(CancellationToken ct, string email)
        {
            await _authenticationEndpoint.ResetPassword(ct, email);
        }
    }
}
