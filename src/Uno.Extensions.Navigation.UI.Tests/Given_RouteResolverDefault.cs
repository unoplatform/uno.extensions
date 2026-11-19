#if !__ANDROID__ && !__IOS__ && !__WASM__ && !WINDOWS && !__MACCATALYST__
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Navigation;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Guards the implicit by-name route lookup of <see cref="RouteResolverDefault"/> end to end, through the resolver's
/// own anchor assembly. Given_RouteTypeIndex in Uno.Extensions.Navigation.Tests covers the index itself.
/// </summary>
[TestClass]
public class Given_RouteResolverDefault
{
	[TestMethod]
	public void When_ViewModelOnlyInOtherLoadContext_Then_NotResolvedAndWarningNamesSearchedContexts()
	{
		var otherContext = new AssemblyLoadContext("OtherAppLoadContext", isCollectible: true);
		try
		{
			var id = Guid.NewGuid().ToString("N");
			LoadEmittedAssembly(otherContext, $"OtherApp{id}", $"OtherApp{id}Model");
			var logs = new CapturingLoggerProvider();
			var resolver = CreateResolver(logs);

			var otherRoute = resolver.FindByPath($"OtherApp{id}");
			var ownRoute = resolver.FindByPath("ImplicitLookupProbe");

			(otherRoute?.ViewModel).Should().BeNull("a type loaded only into another app's load context must not be resolved");
			ownRoute.Should().NotBeNull();
			ownRoute!.ViewModel.Should().Be(typeof(ImplicitLookupProbeViewModel),
				"a type in the resolver's own load context must still be resolved by name");
			logs.Entries.Should().Contain(e =>
				e.Level == LogLevel.Warning
				&& e.Message.Contains($"'OtherApp{id}'")
				&& e.Message.Contains("the default load context")
				&& e.Message.Contains("ViewMap/RouteMap"));
		}
		finally
		{
			otherContext.Unload();
		}
	}

	private static RouteResolverDefault CreateResolver(CapturingLoggerProvider logs)
	{
		var services = new ServiceCollection();
		var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs).SetMinimumLevel(LogLevel.Trace));
		return new RouteResolverDefault(loggerFactory.CreateLogger<RouteResolverDefault>(), new RouteRegistry(services), new ViewRegistry(services));
	}

	private static Assembly LoadEmittedAssembly(AssemblyLoadContext context, string assemblyName, string typeName)
	{
		var builder = new PersistedAssemblyBuilder(new AssemblyName(assemblyName), typeof(object).Assembly);
		var module = builder.DefineDynamicModule(assemblyName);
		module.DefineType($"{assemblyName}.{typeName}", TypeAttributes.Public | TypeAttributes.Class).CreateType();

		using var stream = new MemoryStream();
		builder.Save(stream);
		stream.Position = 0;
		return context.LoadFromStream(stream);
	}

	public class ImplicitLookupProbeViewModel
	{
	}
}
#endif
