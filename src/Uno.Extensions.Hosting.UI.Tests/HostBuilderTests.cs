using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Hosting.Tests;

[TestClass]
public class HostBuilderTests
{
	[TestMethod]
	public void After_Build_ILoggerFactory_Is_AmbientLogger()
	{
		var originalAmbientLoggerFactory = Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory;
		try
		{
			var myLoggerFactory = new LoggerFactory();
			Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = myLoggerFactory;
			var hostBuilder = new HostBuilder();
			var host = hostBuilder.Build();
			var serviceLoggerFactory = host.Services.GetService<ILoggerFactory>();
			serviceLoggerFactory.Should().NotBeNull();
			serviceLoggerFactory.Should().Be(myLoggerFactory);
		}
		finally
		{
			Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = originalAmbientLoggerFactory;
		}
	}
}
