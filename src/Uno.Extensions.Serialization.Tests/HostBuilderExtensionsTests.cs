using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Serialization.Tests;

[TestClass]
public class HostBuilderExtensionsTests
{
	private IServiceProvider _services;
	[TestInitialize]
	public void InitializeTests()
	{
	}

	[TestMethod]
	public void UseSerializationTest()
	{
		var host = Host.CreateDefaultBuilder()
			.UseSerialization()
			.Build();
		_services = host.Services;

		var serializer = _services.GetService<ISerializer>();
		serializer.Should().NotBeNull();
		serializer = _services.GetService<ISerializer<SimpleClass>>();
		serializer.Should().NotBeNull();
	}

	[TestMethod]
	public void UseJsonSerializationResolversTest()
	{
		var host = Host.CreateDefaultBuilder()
			.UseJsonSerializationResolvers(SimpleClassContext.Default)
			.Build();
		_services = host.Services;

		var serializer = _services.GetService<ISerializer>();
		serializer.Should().NotBeNull();
		serializer = _services.GetService<ISerializer<SimpleClass>>();
		serializer.Should().NotBeNull();
	}

	[TestMethod]
	public void UseJsonSerializationResolversTest_WithCustomSerializerOptions()
	{
		var host = Host.CreateDefaultBuilder()
			.UseJsonSerializationResolvers([SimpleClassContext.Default], services =>
			{
				services.ConfigureJsonSerializationOptions(options =>
				{
					options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
				});
			})
			.Build();
		_services = host.Services;

		var serializer = _services.GetService<ISerializer>();
		serializer.Should().NotBeNull();
		serializer = _services.GetService<ISerializer<SimpleClass>>();
		serializer.Should().NotBeNull();
	}

	[TestMethod]
	public void UseJsonSerializationResolversTest_NoConfigure()
	{
		var host = Host.CreateDefaultBuilder()
			.UseJsonSerializationResolvers([SimpleClassContext.Default])
			.Build();
		_services = host.Services;

		var serializer = _services.GetService<ISerializer>();
		serializer.Should().NotBeNull();
		serializer = _services.GetService<ISerializer<SimpleClass>>();
		serializer.Should().NotBeNull();
	}
}
