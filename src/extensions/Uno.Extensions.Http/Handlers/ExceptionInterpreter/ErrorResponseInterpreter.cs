using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Uno.Extensions.Http.Handlers
{
    public class ErrorResponseInterpreter<TResponse> : IErrorResponseInterpreter<TResponse>
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage, TResponse, bool> _isErrorFunc;
        private readonly Func<HttpRequestMessage, HttpResponseMessage, TResponse, Exception> _exceptionFunc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResponseInterpreter{TResponse}"/> class.
        /// </summary>
        /// <param name="isErrorFunc">Is error function.</param>
        /// <param name="exceptionFunc">Exception resolver function.</param>
        public ErrorResponseInterpreter(
            Func<HttpRequestMessage, HttpResponseMessage, TResponse, bool> isErrorFunc,
            Func<HttpRequestMessage, HttpResponseMessage, TResponse, Exception> exceptionFunc
        )
        {
            _isErrorFunc = isErrorFunc ?? throw new ArgumentNullException(nameof(isErrorFunc));
            _exceptionFunc = exceptionFunc ?? throw new ArgumentNullException(nameof(exceptionFunc));
        }

        /// <inheritdoc/>
        public Exception GetException(HttpRequestMessage request, HttpResponseMessage response, TResponse deserializedResponse)
            => _exceptionFunc(request, response, deserializedResponse);

        /// <inheritdoc/>
        public bool IsError(HttpRequestMessage request, HttpResponseMessage response, TResponse deserializedResponse)
            => _isErrorFunc(request, response, deserializedResponse);
    }
}
