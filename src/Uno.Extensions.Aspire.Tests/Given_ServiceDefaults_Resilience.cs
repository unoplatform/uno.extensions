using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http;

namespace Uno.Extensions.Aspire.Tests;

[TestClass]
public class Given_ServiceDefaults_Resilience
{
	private const int AttemptsWithStandardRetries = 4;

	[TestMethod]
	public async Task When_GetFails_Then_Retried()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		using var host = BuildHost(stub);

		await host.CreateTestClient().GetAsync("http://api.test/items");

		stub.Attempts.Should().Be(AttemptsWithStandardRetries);
	}

	[TestMethod]
	public async Task When_PostFails_Then_NotRetried()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		using var host = BuildHost(stub);

		using var content = new StringContent("{}");
		await host.CreateTestClient().PostAsync("http://api.test/items", content);

		stub.Attempts.Should().Be(1);
	}

	[TestMethod]
	public async Task When_ClientRemovesResilienceHandlers_Then_NotRetried()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is the documented per-client opt-out, still marked experimental
		using var host = TestHost.CreateBuilder()
			.UseServiceDefaults()
			.ConfigureServices(services => services
				.WithoutRetryDelay()
				.AddHttpClient(TestHost.ClientName)
				.RemoveAllResilienceHandlers()
				.ConfigurePrimaryHttpMessageHandler(() => stub))
			.Build();
#pragma warning restore EXTEXP0001

		await host.CreateTestClient().GetAsync("http://api.test/items");

		stub.Attempts.Should().Be(1);
	}

	[TestMethod]
	public async Task When_ContainerHasDelegatingHandler_Then_ItRunsOncePerAttempt()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		var counter = new InvocationCounter();
		using var host = TestHost.CreateBuilder()
			.UseServiceDefaults()
			.ConfigureServices((context, services) => services
				.WithoutRetryDelay()
				.AddSingleton(counter)
				.AddTransient<DelegatingHandler, CountingDelegatingHandler>()
				.AddClientWithEndpoint<ITestApi, EndpointOptions>(
					context,
					new EndpointOptions { Url = "http://api.test/", UseNativeHandler = false },
					httpClientFactory: (s, _) => s.AddHttpClient<ITestApi, TestApi>().ConfigurePrimaryHttpMessageHandler(() => stub)))
			.Build();

		await host.Services.GetRequiredService<ITestApi>().GetAsync(CancellationToken.None);

		counter.Count.Should().Be(AttemptsWithStandardRetries);
	}

	[TestMethod]
	public async Task When_CalledTwice_Then_OneResilienceLayer()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		using var host = TestHost.CreateBuilder()
			.UseServiceDefaults()
			.UseServiceDefaults()
			.ConfigureServices(services => services
				.WithoutRetryDelay()
				.AddHttpClient(TestHost.ClientName)
				.ConfigurePrimaryHttpMessageHandler(() => stub))
			.Build();

		await host.CreateTestClient().GetAsync("http://api.test/items");

		stub.Attempts.Should().Be(AttemptsWithStandardRetries);
	}

	private static IHost BuildHost(StubHttpHandler stub) =>
		TestHost.CreateBuilder()
			.UseServiceDefaults()
			.ConfigureServices(services => services
				.WithoutRetryDelay()
				.AddHttpClient(TestHost.ClientName)
				.ConfigurePrimaryHttpMessageHandler(() => stub))
			.Build();
}
