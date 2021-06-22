using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
    public class TestHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendFunction;

        public TestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendFunction)
        {
			_sendFunction = sendFunction ?? throw new ArgumentNullException(nameof(sendFunction));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
			return _sendFunction(request, ct);
		}
	}
}
