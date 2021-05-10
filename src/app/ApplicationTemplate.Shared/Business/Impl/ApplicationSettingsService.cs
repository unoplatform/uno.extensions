using System;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Client;
using Nventive.Persistence;

namespace ApplicationTemplate.Business
{
	public partial class ApplicationSettingsService : IApplicationSettingsService
	{
		private readonly IObservableDataPersister<ApplicationSettings> _dataPersister;

		public ApplicationSettingsService(IObservableDataPersister<ApplicationSettings> dataPersister)
		{
			_dataPersister = dataPersister ?? throw new ArgumentNullException(nameof(dataPersister));
		}

		/// <inheritdoc />
		public async Task<ApplicationSettings> GetCurrent(CancellationToken ct)
		{
			var result = await _dataPersister.Load(ct);

			return result.Value ?? ApplicationSettings.Default;
		}

		/// <inheritdoc />
		public IObservable<ApplicationSettings> GetAndObserveCurrent()
		{
			return _dataPersister.GetAndObserve().Select(r => r.Value ?? ApplicationSettings.Default);
		}

		/// <inheritdoc />
		public async Task CompleteOnboarding(CancellationToken ct)
		{
			await Update(ct, s => s.WithIsOnboardingCompleted(true));
		}

		/// <inheritdoc />
		public async Task SetAuthenticationData(CancellationToken ct, AuthenticationData authenticationData)
		{
			await Update(ct, s => s.WithAuthenticationData(authenticationData));
		}

		/// <inheritdoc />
		public async Task SetFavoriteQuotes(CancellationToken ct, ImmutableDictionary<string, ChuckNorrisQuote> quotes)
		{
			await Update(ct, s => s.WithFavoriteQuotes(quotes));
		}

		/// <inheritdoc />
		public async Task DiscardUserSettings(CancellationToken ct)
		{
			await Update(ct, s => s
				.WithFavoriteQuotes(ImmutableDictionary<string, ChuckNorrisQuote>.Empty)
				.WithAuthenticationData(default(AuthenticationData))
			);
		}

		private async Task Update(CancellationToken ct, Func<ApplicationSettings, ApplicationSettings> updateFunction)
		{
			await _dataPersister.Update(ct, context =>
			{
				var settings = context.GetReadValueOrDefault(ApplicationSettings.Default);

				context.Commit(updateFunction(settings));
			});
		}
	}
}
