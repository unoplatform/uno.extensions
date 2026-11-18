using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.Tests;

[TestClass]
public class Given_ServiceDefaults_ServiceDiscovery
{
	private const string InjectedEndpointKey = "services:api:http:0";
	private const string InjectedEndpoint = "http://resolved.test:5123";

	[TestMethod]
	public async Task When_UrlIsLogical_Then_RequestGoesToInjectedEndpoint()
	{
		var stub = new StubHttpHandler(HttpStatusCode.OK);
		using var host = BuildHost(stub, baseAddress: "https+http://api");

		await host.CreateTestClient().GetAsync("items");

		stub.RequestUris.Should().ContainSingle().Which.Should().Be(new Uri("http://resolved.test:5123/items"));
	}

	[TestMethod]
	public async Task When_UrlIsLiteral_Then_InjectedEndpointIsIgnored()
	{
		var stub = new StubHttpHandler(HttpStatusCode.OK);
		using var host = BuildHost(stub, baseAddress: "http://literal.test");

		await host.CreateTestClient().GetAsync("items");

		stub.RequestUris.Should().ContainSingle().Which.Should().Be(new Uri("http://literal.test/items"));
	}

	private static IHost BuildHost(StubHttpHandler stub, string baseAddress) =>
		TestHost.CreateBuilder((InjectedEndpointKey, InjectedEndpoint))
			.UseServiceDefaults()
			.ConfigureServices(services => services
				.AddHttpClient(TestHost.ClientName, client => client.BaseAddress = new Uri(baseAddress))
				.ConfigurePrimaryHttpMessageHandler(() => stub))
			.Build();
}
