using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
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

	[TestMethod]
	[DataRow("services__settingstest__http__0", true, DisplayName = "Settings added before the service defaults")]
	[DataRow("services__settingstest__http__0", false, DisplayName = "Settings added after the service defaults")]
	[DataRow("SERVICES__SETTINGSTEST__HTTP__0", true, DisplayName = "Upper-case name, as on Android")]
	public async Task When_SettingsAndAspireGiveAnAddress_Then_TheInjectedOneWins(string variable, bool settingsFirst)
	{
		var stub = new StubHttpHandler(HttpStatusCode.OK);
		var settings = new Dictionary<string, string?> { ["services:settingstest:http:0"] = "http://settings.test:1" };
		Environment.SetEnvironmentVariable(variable, "http://injected.test:2");
		try
		{
			var builder = new HostBuilder();
			if (settingsFirst)
			{
				builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings));
			}

			builder.UseServiceDefaults();
			if (!settingsFirst)
			{
				builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings));
			}

			using var host = builder
				.ConfigureServices(services => services
					.AddHttpClient(TestHost.ClientName, client => client.BaseAddress = new Uri("https+http://settingstest"))
					.ConfigurePrimaryHttpMessageHandler(() => stub))
				.Build();

			await host.CreateTestClient().GetAsync("items");

			stub.RequestUris.Should().ContainSingle().Which.Should().Be(new Uri("http://injected.test:2/items"));
		}
		finally
		{
			Environment.SetEnvironmentVariable(variable, null);
		}
	}

	[TestMethod]
	public async Task When_AspireGivesNoAddress_Then_TheSettingsAddressIsUsed()
	{
		var stub = new StubHttpHandler(HttpStatusCode.OK);
		using var host = TestHost.CreateBuilder(("services:fallbacktest:http:0", "http://settings.test:1"))
			.UseServiceDefaults()
			.ConfigureServices(services => services
				.AddHttpClient(TestHost.ClientName, client => client.BaseAddress = new Uri("https+http://fallbacktest"))
				.ConfigurePrimaryHttpMessageHandler(() => stub))
			.Build();

		await host.CreateTestClient().GetAsync("items");

		stub.RequestUris.Should().ContainSingle().Which.Should().Be(new Uri("http://settings.test:1/items"));
	}

	private static IHost BuildHost(StubHttpHandler stub, string baseAddress) =>
		TestHost.CreateBuilder((InjectedEndpointKey, InjectedEndpoint))
			.UseServiceDefaults()
			.ConfigureServices(services => services
				.AddHttpClient(TestHost.ClientName, client => client.BaseAddress = new Uri(baseAddress))
				.ConfigurePrimaryHttpMessageHandler(() => stub))
			.Build();
}
