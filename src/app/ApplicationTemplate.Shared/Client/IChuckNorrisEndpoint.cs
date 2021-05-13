using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace ApplicationTemplate.Client
{
    public interface IChuckNorrisEndpoint
    {
        /// <summary>
        /// Returns a list of quotes that match the <paramref name="searchTerm"/>.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="searchTerm">Search term</param>
        /// <returns>List of quotes</returns>
        [Get("/search")]
        Task<ChuckNorrisResponse> Search(CancellationToken ct, [AliasAs("query")] string searchTerm);
    }
}
