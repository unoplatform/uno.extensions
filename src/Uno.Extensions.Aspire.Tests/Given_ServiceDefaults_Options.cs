using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Trace;
using Uno.Extensions.Http;
using Uno.Extensions.Http.Kiota;

namespace Uno.Extensions.Aspire.Tests;

[TestClass]
public class Given_ServiceDefaults_Options
{
	private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

	[TestMethod]
	public async Task When_ResilienceIsOff_Then_GetIsNotRetriedAndKiotaKeepsItsRetries()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		using var host = TestHost.CreateBuilder()
			.UseServiceDefaults(options => options.UseResilience = false)
			.ConfigureServices(services => services
				.AddHttpClient(TestHost.ClientName)
				.ConfigurePrimaryHttpMessageHandler(() => stub))
			.ConfigureServices((context, services) => services.AddKiotaClient<TestKiotaClient>(
				context,
				new EndpointOptions { Url = "http://kiota.test", UseNativeHandler = false },
				configure: (builder, _) => builder.ConfigurePrimaryHttpMessageHandler(() => new StubHttpHandler(HttpStatusCode.OK))))
			.Build();

		await host.CreateTestClient().GetAsync("http://api.test/items");

		stub.Attempts.Should().Be(1);
		host.Services.GetService<RetryHandlerOption>().Should().BeNull("Kiota keeps its own retries when nothing else retries");
	}

	[TestMethod]
	public async Task When_ResilienceIsConfigured_Then_TheChangeApplies()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		using var host = TestHost.CreateBuilder()
			.UseServiceDefaults(options => options.ConfigureResilience = resilience => resilience.Retry.MaxRetryAttempts = 1)
			.ConfigureServices(services => services
				.WithoutRetryDelay()
				.AddHttpClient(TestHost.ClientName)
				.ConfigurePrimaryHttpMessageHandler(() => stub))
			.Build();

		await host.CreateTestClient().GetAsync("http://api.test/items");

		stub.Attempts.Should().Be(2);
	}

	[TestMethod]
	public void When_TelemetryIsOff_Then_NothingIsRegisteredEvenWithAnEndpoint()
	{
		using var host = TestHost.CreateBuilder((OtlpEndpointKey, "http://localhost:4317"))
			.UseServiceDefaults(options => options.UseTelemetry = false)
			.Build();

		host.Services.GetService<TracerProvider>().Should().BeNull();
	}
}
