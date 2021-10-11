using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
    /// <summary>
    /// This <see cref="HttpMessageHandler"/> reports all
    /// exceptions to the provided <see cref="IExceptionHub"/>.
    /// </summary>

    public class ExceptionHubHandler : DelegatingHandler
    {
        private readonly IExceptionHub _exceptionHub;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHubHandler"/> class.
        /// </summary>
        /// <param name="exceptionHub"><see cref="IExceptionHub"/>.</param>
        public ExceptionHubHandler(IExceptionHub exceptionHub)
        {
            _exceptionHub = exceptionHub ?? throw new ArgumentNullException(nameof(exceptionHub));
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception e)
            {
                _exceptionHub.ReportException(e);

                throw;
            }
        }
    }
}
