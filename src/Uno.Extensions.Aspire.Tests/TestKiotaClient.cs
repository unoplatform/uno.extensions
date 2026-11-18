using Microsoft.Kiota.Abstractions;

namespace Uno.Extensions.Aspire.Tests;

/// <summary>
/// Shaped like a generated Kiota client: one constructor taking the request adapter.
/// </summary>
internal sealed class TestKiotaClient(IRequestAdapter adapter)
{
	public async Task SendAsync(Method method, CancellationToken ct)
	{
		var request = new RequestInformation { HttpMethod = method, URI = new Uri("http://kiota.test/items") };
		using var body = new MemoryStream(new byte[] { 1 });
		if (method != Method.GET)
		{
			request.SetStreamContent(body, "application/octet-stream");
		}

		try
		{
			await adapter.SendNoContentAsync(request, cancellationToken: ct);
		}
		catch (ApiException)
		{
			// The stub always fails; the tests count attempts.
		}
		catch (AggregateException)
		{
			// Kiota's retry handler throws this once its retries are exhausted.
		}
	}
}
