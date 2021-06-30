using System;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Client;
using Uno.Extensions.Configuration;
using Uno.Extensions.Http;

namespace ApplicationTemplate.Business
{
    public partial class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly IWritableOptions<ApplicationSettings> _dataPersister;

        public ApplicationSettingsService(IWritableOptions<ApplicationSettings> dataPersister)
        {
            _dataPersister = dataPersister ?? throw new ArgumentNullException(nameof(dataPersister));
        }

        /// <inheritdoc />
        public async Task<ApplicationSettings> GetCurrent(CancellationToken ct)
        {
            return _dataPersister.Value ?? default;
            //var result = await _dataPersister.Value;

            //return result.Value ?? ApplicationSettings.Default;
        }

        /// <inheritdoc />
        public IObservable<ApplicationSettings> GetAndObserveCurrent()
        {
            return null; // TODO: listen to changes in the IWritableOptions
            //return _dataPersister.GetAndObserve().Select(r => r.Value ?? ApplicationSettings.Default);
        }

        /// <inheritdoc />
        public async Task CompleteOnboarding(CancellationToken ct)
        {
            await Update(s => s.IsOnboardingCompleted = true);
        }

        /// <inheritdoc />
        public async Task SetAuthenticationData(CancellationToken ct, AuthenticationData authenticationData)
        {
            await Update(s => s.AuthenticationData = authenticationData);
        }

        /// <inheritdoc />
        public async Task SetFavoriteQuotes(CancellationToken ct, ImmutableDictionary<string, ChuckNorrisQuote> quotes)
        {
            await Update(s=>s.FavoriteQuotes = quotes);
        }

        /// <inheritdoc />
        public async Task DiscardUserSettings(CancellationToken ct)
        {
            await Update(s =>
            {
                s.FavoriteQuotes = ImmutableDictionary<string, ChuckNorrisQuote>.Empty;
                s.IsOnboardingCompleted = false;
                s.AuthenticationData = default;
            });
        }

        private async Task Update(Action<ApplicationSettings> updateFunction)
        {
            await _dataPersister.Update(updateFunction);
        }
    }
}
