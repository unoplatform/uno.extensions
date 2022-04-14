using System;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Serialization.Tests;

[TestClass]
public class ServiceCollectionExtensionsTests
{
	private IServiceProvider _services;
	[TestInitialize]
	public void InitializeTests()
	{
	}

	[TestMethod]
	public void AddSystemTextJsonSerializationTest()
	{
		var services = new ServiceCollection();
		services.AddSystemTextJsonSerialization();
		_services = services.BuildServiceProvider();

		var serializer = _services.GetService<ISerializer>();
		serializer.Should().NotBeNull();
		serializer = _services.GetService<ISerializer<SimpleClass>>();
		serializer.Should().NotBeNull();
		var streamSerializer = _services.GetService<IStreamSerializer>();
		streamSerializer.Should().NotBeNull();
		streamSerializer = _services.GetService<IStreamSerializer<SimpleClass>>();
		streamSerializer.Should().NotBeNull();
	}
}
