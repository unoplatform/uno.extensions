using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry.Trace;

namespace Uno.Extensions.Aspire.Tests;

[TestClass]
public class Given_ServiceDefaults_Telemetry
{
	private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

	[TestMethod]
	public void When_NoOtlpEndpoint_Then_NoTelemetryIsRegistered()
	{
		using var host = TestHost.CreateBuilder().UseServiceDefaults().Build();

		host.Services.GetService<TracerProvider>().Should().BeNull();
		host.Services.GetServices<IHostedService>().Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_OtlpEndpoint_Then_HttpClientCallIsTraced()
	{
		using var server = LoopbackServer.Start();
		var exported = new List<Activity>();
		using var host = TestHost.CreateBuilder((OtlpEndpointKey, server.Address.ToString()))
			.UseServiceDefaults()
			.ConfigureServices(services => services
				.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(exported))
				.AddHttpClient(TestHost.ClientName))
			.Build();
		await host.StartAsync();

		await host.CreateTestClient().GetAsync(server.Address);
		host.Services.GetRequiredService<TracerProvider>().ForceFlush();

		exported.Should().Contain(activity => activity.Kind == ActivityKind.Client);
		await host.StopAsync();
	}
}
