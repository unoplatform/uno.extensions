using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationTemplate.Client
{
    public class PostEndpointException : Exception
    {
        public PostEndpointException()
        {
        }

        public PostEndpointException(string message)
            : base(message)
        {
        }

        public PostEndpointException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public PostEndpointException(PostErrorResponse errorResponse)
        {
        }
    }
}
