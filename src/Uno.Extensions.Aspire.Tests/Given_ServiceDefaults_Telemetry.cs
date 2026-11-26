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
			.UseEnvironment(Environments.Development)
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

	[TestMethod]
	public void When_OtlpEndpointOutsideDevelopment_Then_NoTelemetryIsRegistered()
	{
		using var host = TestHost.CreateBuilder((OtlpEndpointKey, "http://localhost:4317"))
			.UseEnvironment(Environments.Production)
			.UseServiceDefaults()
			.Build();

		host.Services.GetService<TracerProvider>().Should().BeNull("a released app exports nothing unless it asks to");
	}

	[TestMethod]
	public void When_ExportInAllEnvironments_Then_TelemetryIsRegisteredOutsideDevelopment()
	{
		using var host = TestHost.CreateBuilder((OtlpEndpointKey, "http://localhost:4317"))
			.UseEnvironment(Environments.Production)
			.UseServiceDefaults(options => options.ExportTelemetryInAllEnvironments = true)
			.Build();

		host.Services.GetService<TracerProvider>().Should().NotBeNull();
	}
}
