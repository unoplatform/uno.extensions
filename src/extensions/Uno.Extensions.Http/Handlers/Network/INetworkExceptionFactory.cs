using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Http.Handlers
{
    public interface INetworkExceptionFactory
    {
        /// <summary>
        /// Creates a new network exception.
        /// </summary>
        /// <param name="innerException">Inner exception.</param>
        /// <returns>Network exception.</returns>
        Exception CreateNetworkException(Exception innerException);
    }
}
