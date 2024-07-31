using Uno.Extensions.Http;

namespace TestHarness.Ext.Http.Endpoints;

public class CustomEndpointOptions: EndpointOptions
{
	public string? ApiKey { get; set; }
}
