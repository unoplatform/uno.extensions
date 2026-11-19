namespace Uno.Extensions.Aspire.Tests;

/// <summary>
/// Stands in for a container-registered handler such as authentication.
/// </summary>
internal sealed class CountingDelegatingHandler(InvocationCounter counter) : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		counter.Increment();
		return base.SendAsync(request, cancellationToken);
	}
}
