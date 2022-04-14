using System;
using System.IO;
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
}
