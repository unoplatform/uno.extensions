using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http;
using Uno.Extensions.Http.Kiota;

namespace Uno.Extensions.Aspire.Tests;

[TestClass]
public class Given_ServiceDefaults_KiotaRetry
{
	private const int AttemptsWithOneRetryLayer = 4;

	[TestMethod]
	public async Task When_ServiceDefaults_Then_GetHasOneRetryLayer()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		using var host = TestHost.CreateBuilder()
			.UseServiceDefaults()
			.ConfigureServices((context, services) => AddKiotaClient(context, services, stub))
			.Build();

		await SendAsync(host, Method.GET);

		stub.Attempts.Should().Be(AttemptsWithOneRetryLayer);
	}

	[TestMethod]
	public async Task When_ServiceDefaultsCalledAfterClientRegistration_Then_GetHasOneRetryLayer()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		using var host = TestHost.CreateBuilder()
			.ConfigureServices((context, services) => AddKiotaClient(context, services, stub))
			.UseServiceDefaults()
			.Build();

		await SendAsync(host, Method.GET);

		stub.Attempts.Should().Be(AttemptsWithOneRetryLayer);
	}

	[TestMethod]
	public async Task When_ServiceDefaults_Then_PostIsNotRetried()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		using var host = TestHost.CreateBuilder()
			.UseServiceDefaults()
			.ConfigureServices((context, services) => AddKiotaClient(context, services, stub))
			.Build();

		await SendAsync(host, Method.POST);

		stub.Attempts.Should().Be(1);
	}

	[TestMethod]
	public async Task When_NoServiceDefaults_Then_KiotaStillRetriesPost()
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		using var host = TestHost.CreateBuilder()
			.ConfigureServices((context, services) => AddKiotaClient(context, services, stub))
			.Build();

		await SendAsync(host, Method.POST);

		stub.Attempts.Should().Be(AttemptsWithOneRetryLayer);
		host.Services.GetService<RetryHandlerOption>().Should().BeNull();
	}

	[TestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public async Task When_AppRegistersRetryOption_Then_ItWins(bool registerBeforeClient)
	{
		var stub = new StubHttpHandler(HttpStatusCode.ServiceUnavailable);
		var appOption = new RetryHandlerOption { MaxRetry = 1 };
		using var host = TestHost.CreateBuilder()
			.UseServiceDefaults()
			.ConfigureServices((context, services) =>
			{
				if (registerBeforeClient)
				{
					services.AddSingleton(appOption);
				}

				AddKiotaClient(context, services, stub);

				if (!registerBeforeClient)
				{
					services.AddSingleton(appOption);
				}
			})
			.Build();

		await SendAsync(host, Method.POST);

		// POST: the resilience handler does not retry, so the two attempts are Kiota's, under the app's option.
		stub.Attempts.Should().Be(2);
	}

	private static void AddKiotaClient(HostBuilderContext context, IServiceCollection services, StubHttpHandler stub) =>
		services
			.WithoutRetryDelay()
			.AddKiotaClient<TestKiotaClient>(
				context,
				new EndpointOptions { Url = "http://kiota.test", UseNativeHandler = false },
				configure: (builder, _) => builder.ConfigurePrimaryHttpMessageHandler(() => stub));

	private static Task SendAsync(IHost host, Method method) =>
		host.Services.GetRequiredService<TestKiotaClient>().SendAsync(method, CancellationToken.None);
}
