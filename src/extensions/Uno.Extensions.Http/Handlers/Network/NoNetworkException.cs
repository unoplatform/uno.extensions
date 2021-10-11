using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Http.Handlers
{
    public class NoNetworkException : Exception
    {
        public NoNetworkException()
        {
        }

        public NoNetworkException(string message)
            : base(message)
        {
        }

        public NoNetworkException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
