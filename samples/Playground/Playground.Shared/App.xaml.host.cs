using Uno.Extensions.Navigation.UI;

namespace Playground;

public sealed partial class App : Application
{
	public IHost? _host;
}

public class DebugHttpHandler : DelegatingHandler
{
	public DebugHttpHandler(HttpMessageHandler? innerHandler = null)
		: base(innerHandler ?? new HttpClientHandler())
	{
	}

	protected async override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		return await base.SendAsync(request, cancellationToken);
	}
}
