using System;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Client;
using Uno.Extensions.Http;
using Uno.Extensions.Http.Handlers;

namespace ApplicationTemplate.Business
{
    public partial class AuthenticationService : IAuthenticationService, IAuthenticationTokenProvider<AuthenticationData>
    {
        private readonly ISubject<Unit> _sessionExpired = new Subject<Unit>();
        private readonly IApplicationSettingsService _applicationSettingsService;
        private readonly IAuthenticationEndpoint _authenticationEndpoint;

        public AuthenticationService(
            IApplicationSettingsService applicationSettingsService,
            IAuthenticationEndpoint authenticationEndpoint)
        {
            _applicationSettingsService = applicationSettingsService ?? throw new ArgumentNullException(nameof(applicationSettingsService));
            _authenticationEndpoint = authenticationEndpoint ?? throw new ArgumentNullException(nameof(authenticationEndpoint));
        }

        /// <inheritdoc/>
        public IObservable<AuthenticationData> GetAndObserveAuthenticationData()
        {
            return _applicationSettingsService
                .GetAndObserveCurrent()
                .Select(s => s.AuthenticationData);
        }

        /// <inheritdoc/>
        public IObservable<bool> GetAndObserveIsAuthenticated()
        {
            return GetAndObserveAuthenticationData()
                .Select(s => s != default(AuthenticationData));
        }

        /// <inheritdoc/>
        public IObservable<Unit> ObserveSessionExpired() => _sessionExpired;

        /// <inheritdoc/>
        public async Task<AuthenticationData> Login(CancellationToken ct, string email, string password)
        {
            var authenticationData = await _authenticationEndpoint.Login(ct, email, password);

            await _applicationSettingsService.SetAuthenticationData(ct, authenticationData);

            return authenticationData;
        }

        /// <inheritdoc/>
        public async Task Logout(CancellationToken ct)
        {
            await _applicationSettingsService.DiscardUserSettings(ct);
        }

        /// <inheritdoc/>
        public async Task<AuthenticationData> GetToken(CancellationToken ct, HttpRequestMessage request)
        {
            var settings = await _applicationSettingsService.GetAndObserveCurrent().FirstAsync(ct);

            return settings.AuthenticationData;
        }

        /// <inheritdoc/>
        public async Task<AuthenticationData> RefreshToken(CancellationToken ct, HttpRequestMessage request, AuthenticationData unauthorizedToken)
        {
            var authenticationData = await _authenticationEndpoint.RefreshToken(ct, unauthorizedToken);

            await _applicationSettingsService.SetAuthenticationData(ct, authenticationData);

            return authenticationData;
        }

        /// <inheritdoc/>
        public async Task NotifySessionExpired(CancellationToken ct, HttpRequestMessage request, AuthenticationData unauthorizedToken)
        {
            await _applicationSettingsService.SetAuthenticationData(ct, default(AuthenticationData));

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
