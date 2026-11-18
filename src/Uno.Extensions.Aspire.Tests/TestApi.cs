namespace Uno.Extensions.Aspire.Tests;

internal sealed class TestApi(HttpClient client) : ITestApi
{
	public Task<HttpResponseMessage> GetAsync(CancellationToken ct) => client.GetAsync("items", ct);
}
