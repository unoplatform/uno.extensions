using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
		var context = new HostBuilderContext(new Dictionary<object,object>());
		var services = new ServiceCollection();
		services.AddSystemTextJsonSerialization(context);
		_services = services.BuildServiceProvider();

		var serializer = _services.GetService<ISerializer>();
		serializer.Should().NotBeNull();
		serializer = _services.GetService<ISerializer<SimpleClass>>();
		serializer.Should().NotBeNull();
	}
}
