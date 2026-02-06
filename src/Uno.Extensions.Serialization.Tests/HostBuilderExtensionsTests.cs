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
		var hostBuilder = Host.CreateDefaultBuilder();
		var services = new ServiceCollection();

#if WITH_AOT_TRIMMING
		hostBuilder.UseSerialization([SimpleClassContext.Default]);
#else   // !WITH_AOT_TRIMMING
		hostBuilder.UseSerialization();
#endif  // WITH_AOT_TRIMMING
		var host = hostBuilder.Build();

		_services = host.Services;
	}

	[TestMethod]
	public void UseSerializationTest()
	{
		var serializer = _services.GetService<ISerializer>();
		serializer.Should().NotBeNull();
		serializer = _services.GetService<ISerializer<SimpleClass>>();
		serializer.Should().NotBeNull();
	}

	[TestMethod]
	public void UseSerialization_WithCustomSerializerOptions()
	{
		var host = Host.CreateDefaultBuilder()
			.UseSerialization([SimpleClassContext.Default], services =>
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
}
