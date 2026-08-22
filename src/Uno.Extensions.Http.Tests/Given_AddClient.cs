using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.Extensions.Http;

namespace Uno.Extensions.Http.Tests;

/// <summary>
/// Registration coverage for AddClient/AddClientWithEndpoint, including the ServiceLifetime
/// overloads added by spec 013.
/// </summary>
[TestClass]
public class Given_AddClient
{
	private const string EndpointName = "TestClient";
	private const string EndpointUrl = "https://unit.test/";

	public interface IEchoClient
	{
		HttpClient Http { get; }
	}

	public class EchoClient : IEchoClient
	{
		public EchoClient(HttpClient client) => Http = client;

		public HttpClient Http { get; }
	}

	public class TestEndpointOptions : EndpointOptions
	{
		public string? ApiKey { get; set; }
	}

	private static IHost BuildHost(Action<HostBuilderContext, IServiceCollection> registerClient) =>
		Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration(configuration => configuration
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					[$"{EndpointName}:Url"] = EndpointUrl,
					// The native handler only exists in a running Uno app; a bare host has no
					// HttpMessageHandler registration for it to resolve.
					[$"{EndpointName}:UseNativeHandler"] = "false",
					[$"{EndpointName}:ApiKey"] = "test-api-key",
				}))
			.ConfigureServices(registerClient)
			.Build();

	[TestMethod]
	public void When_Default_Then_TransientWithConfiguredEndpoint()
	{
		using var host = BuildHost((context, services) =>
			services.AddClient<EchoClient>(context, name: EndpointName));

		var first = host.Services.GetRequiredService<EchoClient>();
		var second = host.Services.GetRequiredService<EchoClient>();

		first.Should().NotBeSameAs(second, "typed clients default to transient");
		first.Http.BaseAddress.Should().Be(new Uri(EndpointUrl));
	}

	[TestMethod]
	public void When_Singleton_Then_SameInstanceWithConfiguredEndpoint()
	{
		using var host = BuildHost((context, services) =>
			services.AddClient<EchoClient>(context, ServiceLifetime.Singleton, name: EndpointName));

		var first = host.Services.GetRequiredService<EchoClient>();
		var second = host.Services.GetRequiredService<EchoClient>();

		first.Should().BeSameAs(second);
		first.Http.BaseAddress.Should().Be(
			new Uri(EndpointUrl),
			"the endpoint pipeline must still apply to a non-transient client");
	}

	[TestMethod]
	public void When_Scoped_Then_SameInstanceWithinScopeOnly()
	{
		using var host = BuildHost((context, services) =>
			services.AddClient<EchoClient>(context, ServiceLifetime.Scoped, name: EndpointName));

		using var firstScope = host.Services.CreateScope();
		using var secondScope = host.Services.CreateScope();

		var first = firstScope.ServiceProvider.GetRequiredService<EchoClient>();

		firstScope.ServiceProvider.GetRequiredService<EchoClient>().Should().BeSameAs(first);
		secondScope.ServiceProvider.GetRequiredService<EchoClient>().Should().NotBeSameAs(first);
	}

	[TestMethod]
	public void When_SingletonWithInterface_Then_ResolvesThroughInterface()
	{
		using var host = BuildHost((context, services) =>
			services.AddClient<IEchoClient, EchoClient>(context, ServiceLifetime.Singleton, name: EndpointName));

		var first = host.Services.GetRequiredService<IEchoClient>();
		var second = host.Services.GetRequiredService<IEchoClient>();

		first.Should().BeSameAs(second);
		first.Should().BeOfType<EchoClient>();
		first.Http.BaseAddress.Should().Be(new Uri(EndpointUrl));
	}

	[TestMethod]
	public void When_CustomEndpointOptions_Then_BoundFromConfiguration()
	{
		TestEndpointOptions? bound = null;

		using var host = BuildHost((context, services) =>
			services.AddClientWithEndpoint<EchoClient, TestEndpointOptions>(
				context,
				ServiceLifetime.Singleton,
				name: EndpointName,
				configure: (builder, options) =>
				{
					bound = options;
					return builder;
				}));

		// The configure callback runs at registration time; building the host was enough.
		bound.Should().NotBeNull();
		bound!.ApiKey.Should().Be("test-api-key");
		bound.Url.Should().Be(EndpointUrl);

		host.Services.GetRequiredService<EchoClient>().Http.BaseAddress.Should().Be(new Uri(EndpointUrl));
	}
}
