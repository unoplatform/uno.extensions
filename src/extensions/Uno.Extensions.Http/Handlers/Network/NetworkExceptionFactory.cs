using System;

namespace Uno.Extensions.Http.Handlers
{
    public class NetworkExceptionFactory : INetworkExceptionFactory
    {
        public Exception CreateNetworkException(Exception innerException)
        {
            return new NoNetworkException("There is no network", innerException);
        }
    }
}
