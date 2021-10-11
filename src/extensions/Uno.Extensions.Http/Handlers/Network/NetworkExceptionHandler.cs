using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
    /// <summary>
    /// This <see cref="HttpMessageHandler"/> throws a specific type of
    /// exception if the request fails and there is no network.
    /// </summary>
    public class NetworkExceptionHandler : DelegatingHandler
    {
        private readonly INetworkAvailabilityChecker _networkAvailabilityChecker;

        private readonly INetworkExceptionFactory _networkExceptionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkExceptionHandler"/> class.
        /// </summary>
        /// <param name="networkAvailabilityChecker"><see cref="INetworkAvailabilityChecker"/>.</param>
        /// <param name="networkExceptionFactory"><see cref="INetworkExceptionFactory"/>.</param>
        public NetworkExceptionHandler(
            INetworkAvailabilityChecker networkAvailabilityChecker,
            INetworkExceptionFactory networkExceptionFactory = null
        )
        {
            _networkAvailabilityChecker = networkAvailabilityChecker ?? throw new ArgumentNullException(nameof(networkAvailabilityChecker));
            _networkExceptionFactory = networkExceptionFactory ?? new NetworkExceptionFactory();
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception e)
            {
                if (!await _networkAvailabilityChecker.CheckIsNetworkAvailable(cancellationToken))
                {
                    var noNetworkException = _networkExceptionFactory.CreateNetworkException(e);

                    if (noNetworkException == null)
                    {
                        throw new InvalidOperationException("No network exception cannot be null.", e);
                    }

                    throw noNetworkException;
                }

                throw;
            }
        }
    }
}
