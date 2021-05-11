using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationTemplate.Client
{
    public class ChuckNorrisException : Exception
    {
        public ChuckNorrisException()
        {
        }

        public ChuckNorrisException(string message)
            : base(message)
        {
        }

        public ChuckNorrisException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
