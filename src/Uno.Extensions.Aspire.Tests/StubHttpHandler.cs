using System.Net;
using System.Net.Http.Headers;

namespace Uno.Extensions.Aspire.Tests;

/// <summary>
/// Terminal handler that records every request it receives and answers with a fixed status.
/// </summary>
internal sealed class StubHttpHandler(HttpStatusCode status) : HttpMessageHandler
{
	private readonly List<Uri?> _requestUris = new();

	public int Attempts => _requestUris.Count;

	public IReadOnlyList<Uri?> RequestUris => _requestUris;

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		_requestUris.Add(request.RequestUri);

		var response = new HttpResponseMessage(status) { RequestMessage = request };
		// Retry-After: 0 makes every retry layer retry immediately, so the tests do not wait.
		response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
		response.Headers.TryAddWithoutValidation("Set-Cookie", "session=server-secret");
		return Task.FromResult(response);
	}
}
