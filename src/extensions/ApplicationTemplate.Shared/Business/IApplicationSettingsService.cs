using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Client;

namespace ApplicationTemplate.Business
{
    public interface IApplicationSettingsService
    {
        /// <summary>
        /// Gets and observes the current <see cref="ApplicationSettings"/>.
        /// </summary>
        /// <returns>Current <see cref="ApplicationSettings"/></returns>
        IObservable<ApplicationSettings> GetAndObserveCurrent();

        /// <summary>
        /// Gets the current <see cref="ApplicationSettings"/>.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <returns>Current <see cref="ApplicationSettings"/></returns>
        Task<ApplicationSettings> GetCurrent(CancellationToken ct);

        /// <summary>
        /// Discards any settings that are related to the user.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <returns><see cref="Task"/></returns>
        Task DiscardUserSettings(CancellationToken ct);

        /// <summary>
        /// Flags that the onboarding has been completed.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <returns><see cref="Task"/></returns>
        Task CompleteOnboarding(CancellationToken ct);

        /// <summary>
        /// Sets the current <see cref="AuthenticationData"/>.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="authenticationData"><see cref="AuthenticationData"/></param>
        /// <returns><see cref="Task"/></returns>
        Task SetAuthenticationData(CancellationToken ct, AuthenticationData authenticationData);

        /// <summary>
        /// Sets the favorite quotes.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="quotes">Favorite quotes</param>
        /// <returns><see cref="Task"/></returns>
        Task SetFavoriteQuotes(CancellationToken ct, ImmutableDictionary<string, ChuckNorrisQuote> quotes);
    }
}
