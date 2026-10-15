using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace Uno.Extensions.Navigation.Tests;

/// <summary>
/// Guards which assemblies <see cref="RouteTypeIndex"/> indexes for the implicit by-name route lookup.
/// </summary>
/// <remarks>
/// A downstream host can run several apps in one process, each in its own collectible <see cref="AssemblyLoadContext"/>.
/// The implicit lookup must only see the resolver's own load context and the default one, never another app's context,
/// which may be unloading.
/// </remarks>
[TestClass]
public class Given_RouteTypeIndex
{
	[TestMethod]
	public void When_TypeOnlyInOtherLoadContext_Then_NotIndexed()
	{
		var otherContext = new AssemblyLoadContext("OtherLoadContext", isCollectible: true);
		try
		{
			LoadEmittedAssembly(otherContext, "OtherLoadContextApp", "OtherLoadContextOnlyModel");

			var index = IndexFor(AssemblyLoadContext.Default);

			index.Should().NotContainKey("OtherLoadContextOnlyModel",
				"a type loaded only into another app's load context must not be indexed");
			index.Should().Contain(nameof(DefaultLoadContextOnlyModel), typeof(DefaultLoadContextOnlyModel));
		}
		finally
		{
			otherContext.Unload();
		}
	}

	[TestMethod]
	public void When_OwnLoadContext_Then_OwnTypeWinsOverDefault()
	{
		var appContext = new AssemblyLoadContext("AppLoadContext", isCollectible: true);
		try
		{
			var appAssembly = LoadEmittedAssembly(appContext, "OwnLoadContextApp", nameof(SharedNameModel));

			var index = IndexFor(appContext);

			index[nameof(SharedNameModel)].Assembly.Should().BeSameAs(appAssembly,
				"the app's own type must win over a same-named type in the default load context");
			index.Should().Contain(nameof(DefaultLoadContextOnlyModel), typeof(DefaultLoadContextOnlyModel));
		}
		finally
		{
			appContext.Unload();
		}
	}

	[TestMethod]
	public void When_AssemblyAlsoInOwnLoadContext_Then_DefaultCopyIgnored()
	{
		var appContext = new AssemblyLoadContext("AppLoadContextWithOwnCopy", isCollectible: true);
		try
		{
			// The default context can never unload, so unique names keep a rerun in the same process from finding an
			// earlier run's default copy, which this run's own context no longer shadows.
			var id = Guid.NewGuid().ToString("N");
			var assemblyName = $"SameSimpleNameLib{id}";
			LoadEmittedAssembly(AssemblyLoadContext.Default, assemblyName, $"DefaultCopyOnly{id}Model");
			var ownCopy = LoadEmittedAssembly(appContext, assemblyName, $"OwnCopyOnly{id}Model");

			var index = IndexFor(appContext);

			index.Should().NotContainKey($"DefaultCopyOnly{id}Model",
				"the default context's copy of an assembly the own context also has must not be indexed");
			index[$"OwnCopyOnly{id}Model"].Assembly.Should().BeSameAs(ownCopy);
		}
		finally
		{
			appContext.Unload();
		}
	}

	[TestMethod]
	public void When_AssemblyAlsoInOwnLoadContextWithOtherCasing_Then_DefaultCopyIgnored()
	{
		var appContext = new AssemblyLoadContext("AppLoadContextWithOwnCopyOtherCasing", isCollectible: true);
		try
		{
			// Simple names are compared case-insensitively by the loader, so a copy whose name differs only in casing is
			// still the same assembly.
			var id = Guid.NewGuid().ToString("N");
			LoadEmittedAssembly(AssemblyLoadContext.Default, $"samesimplenamelib{id}", $"DefaultCopyOnly{id}Model");
			LoadEmittedAssembly(appContext, $"SameSimpleNameLib{id}", $"OwnCopyOnly{id}Model");

			var index = IndexFor(appContext);

			index.Should().NotContainKey($"DefaultCopyOnly{id}Model",
				"assembly simple names differing only in casing name the same assembly");
		}
		finally
		{
			appContext.Unload();
		}
	}

	private static IDictionary<string, Type> IndexFor(AssemblyLoadContext ownContext)
		=> RouteTypeIndex.Build(RouteTypeIndex.OwnAndDefaultContextAssemblies(ownContext));

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

	public class DefaultLoadContextOnlyModel
	{
	}

	public class SharedNameModel
	{
	}
}
