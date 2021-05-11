using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using DynamicData;

namespace ApplicationTemplate.Business
{
    public interface IChuckNorrisService
    {
        /// <summary>
        /// Returns a list of quotes that match the <paramref name="searchTerm"/>.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="searchTerm">Search term</param>
        /// <returns>List of quotes</returns>
        Task<ChuckNorrisQuote[]> Search(CancellationToken ct, string searchTerm);

        ///// <summary>
        ///// Returns the list of favorite quotes.
        ///// </summary>
        ///// <param name="ct"><see cref="CancellationToken"/></param>
        ///// <returns>List of favorite quotes</returns>
        //Task<IObservableList<ChuckNorrisQuote>> GetFavorites(CancellationToken ct);

        /// <summary>
        /// Sets whether or not a quote is favorite.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="quote"><see cref="ChuckNorrisQuote"/></param>
        /// <param name="isFavorite">Is favorite or not</param>
        /// <returns><see cref="Task"/></returns>
        Task SetIsFavorite(CancellationToken ct, ChuckNorrisQuote quote, bool isFavorite);
    }
}
