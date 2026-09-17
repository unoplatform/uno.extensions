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
/// overloads added by spec 014.
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

	/// <summary>Named after the configuration section, for the no-name path.</summary>
	public class TestClient : EchoClient
	{
		public TestClient(HttpClient client) : base(client)
		{
		}
	}

	public static class Alpha
	{
		public class Api : EchoClient
		{
			public Api(HttpClient client) : base(client)
			{
			}
		}
	}

	public static class Beta
	{
		public class Api : EchoClient
		{
			public Api(HttpClient client) : base(client)
			{
			}
		}
	}

	[TestMethod]
	public void When_TransientLifetime_Then_SameAsNoLifetime()
	{
		using var host = BuildHost((context, services) =>
			services.AddClient<EchoClient>(context, ServiceLifetime.Transient, name: EndpointName));

		var first = host.Services.GetRequiredService<EchoClient>();

		first.Should().NotBeSameAs(host.Services.GetRequiredService<EchoClient>());
		first.Http.BaseAddress.Should().Be(new Uri(EndpointUrl));
	}

	[TestMethod]
	public void When_SingletonWithoutName_Then_SectionIsTheTypeName()
	{
		using var host = BuildHost((context, services) =>
			services.AddClient<TestClient>(context, ServiceLifetime.Singleton));

		host.Services.GetRequiredService<TestClient>().Http.BaseAddress.Should().Be(
			new Uri(EndpointUrl),
			"with no name the configuration section is the type's name, as for a transient client");
	}

	/// <summary>
	/// Two clients with the same short type name must not share an HttpClient pipeline: whichever
	/// registered last would set the base address - and the delegating handlers, authorization
	/// included - for both.
	/// </summary>
	[TestMethod]
	public void When_TwoSingletonsShareAShortTypeName_Then_PipelinesStaySeparate()
	{
		using var host = BuildHost((context, services) => services
			.AddClient<Alpha.Api>(context, ServiceLifetime.Singleton, new EndpointOptions { Url = "https://alpha.test/", UseNativeHandler = false })
			.AddClient<Beta.Api>(context, ServiceLifetime.Singleton, new EndpointOptions { Url = "https://beta.test/", UseNativeHandler = false }));

		host.Services.GetRequiredService<Alpha.Api>().Http.BaseAddress.Should().Be(new Uri("https://alpha.test/"));
		host.Services.GetRequiredService<Beta.Api>().Http.BaseAddress.Should().Be(new Uri("https://beta.test/"));
	}

	[TestMethod]
	public void When_SingletonInterfaceWithoutImplementation_Then_RejectedAtRegistration()
	{
		var act = () => BuildHost((context, services) =>
			services.AddClient<IEchoClient>(context, ServiceLifetime.Singleton, name: EndpointName));

		act.Should().Throw<ArgumentException>().WithMessage("*IEchoClient*AddClient<TInterface, TImplementation>*");
	}

	/// <summary>
	/// Transient "behaves exactly like the overloads without a lifetime" - which accept an interface
	/// (Refit and Kiota supply the implementation through their own registration).
	/// </summary>
	[TestMethod]
	public void When_TransientInterfaceWithoutImplementation_Then_RegistersLikeNoLifetime()
	{
		var act = () => BuildHost((context, services) =>
			services.AddClient<IEchoClient>(context, ServiceLifetime.Transient, name: EndpointName)).Dispose();

		act.Should().NotThrow();
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
