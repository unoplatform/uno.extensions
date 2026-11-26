namespace Uno.Extensions.Aspire.Tests;

internal interface ITestApi
{
	Task<HttpResponseMessage> GetAsync(CancellationToken ct);
}
