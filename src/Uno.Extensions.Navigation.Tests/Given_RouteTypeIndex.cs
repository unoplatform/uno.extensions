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

			var index = IndexFor(typeof(Given_RouteTypeIndex).Assembly);

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
	public void When_TypeOnlyInSiblingLoadContext_Then_NotIndexed()
	{
		var appContext = new AssemblyLoadContext("AppLoadContextWithSibling", isCollectible: true);
		var siblingContext = new AssemblyLoadContext("SiblingLoadContext", isCollectible: true);
		try
		{
			var appAssembly = LoadEmittedAssembly(appContext, "AppWithSibling", "AppWithSiblingModel");
			LoadEmittedAssembly(siblingContext, "SiblingApp", "SiblingLoadContextOnlyModel");

			var index = IndexFor(appAssembly);

			index.Should().NotContainKey("SiblingLoadContextOnlyModel",
				"a type loaded only into another app's load context must not be indexed, even while that context is alive");
			index["AppWithSiblingModel"].Assembly.Should().BeSameAs(appAssembly);
		}
		finally
		{
			appContext.Unload();
			siblingContext.Unload();
		}
	}

	[TestMethod]
	public void When_OwnLoadContext_Then_OwnTypeWinsOverDefault()
	{
		var appContext = new AssemblyLoadContext("AppLoadContext", isCollectible: true);
		try
		{
			var appAssembly = LoadEmittedAssembly(appContext, "OwnLoadContextApp", nameof(SharedNameModel));

			var index = IndexFor(appAssembly);

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

			var index = IndexFor(ownCopy);

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
			// The runtime's binder matches simple names case-insensitively, so a copy whose name differs only in casing
			// stands for the same assembly. LoadFromStream bypasses the binder, which is what lets both copies load here.
			var id = Guid.NewGuid().ToString("N");
			LoadEmittedAssembly(AssemblyLoadContext.Default, $"samesimplenamelib{id}", $"DefaultCopyOnly{id}Model");
			var ownCopy = LoadEmittedAssembly(appContext, $"SameSimpleNameLib{id}", $"OwnCopyOnly{id}Model");

			var index = IndexFor(ownCopy);

			index.Should().NotContainKey($"DefaultCopyOnly{id}Model",
				"assembly simple names differing only in casing name the same assembly");
			index[$"OwnCopyOnly{id}Model"].Assembly.Should().BeSameAs(ownCopy);
		}
		finally
		{
			appContext.Unload();
		}
	}

	[TestMethod]
	public void When_SomeTypesFailToLoad_Then_LoadableTypesIndexed()
	{
		var appContext = new AssemblyLoadContext("AppLoadContextWithBrokenType", isCollectible: true);
		try
		{
			var id = Guid.NewGuid().ToString("N");
			var appAssembly = LoadEmittedAssemblyWithMissingBase(appContext, $"PartlyLoadableApp{id}", $"Loadable{id}Model", $"Broken{id}Model");
			var failures = new List<(Assembly Assembly, Exception Error)>();

			var (_, index) = RouteTypeIndex.For(appAssembly, (assembly, error) => failures.Add((assembly, error)));

			index[$"Loadable{id}Model"].Assembly.Should().BeSameAs(appAssembly,
				"one type with a missing dependency must not hide the rest of its assembly");
			index.Should().NotContainKey($"Broken{id}Model");
			failures.Should().ContainSingle().Which.Assembly.Should().BeSameAs(appAssembly);
			failures[0].Error.Should().BeOfType<ReflectionTypeLoadException>();
			AssemblyExtensions.Excludes.Should().NotContain(appAssembly.FullName,
				"an assembly whose types partly load must still be searched by later lookups");
		}
		finally
		{
			appContext.Unload();
		}
	}

	private static IDictionary<string, Type> IndexFor(Assembly anchor)
		=> RouteTypeIndex.For(anchor).Types;

	private static Assembly LoadEmittedAssembly(AssemblyLoadContext context, string assemblyName, string typeName)
	{
		var builder = new PersistedAssemblyBuilder(new AssemblyName(assemblyName), typeof(object).Assembly);
		var module = builder.DefineDynamicModule(assemblyName);
		module.DefineType($"{assemblyName}.{typeName}", TypeAttributes.Public | TypeAttributes.Class).CreateType();

		return Load(context, builder);
	}

	/// <summary>
	/// Emits an assembly with a loadable type and a type whose base class lives in an assembly that is never loaded,
	/// so <see cref="Assembly.GetTypes"/> throws a <see cref="ReflectionTypeLoadException"/>.
	/// </summary>
	private static Assembly LoadEmittedAssemblyWithMissingBase(AssemblyLoadContext context, string assemblyName, string loadableTypeName, string brokenTypeName)
	{
		var missingName = $"{assemblyName}.Missing";
		var missingBuilder = new PersistedAssemblyBuilder(new AssemblyName(missingName), typeof(object).Assembly);
		var missingBase = missingBuilder.DefineDynamicModule(missingName)
			.DefineType($"{missingName}.MissingBase", TypeAttributes.Public | TypeAttributes.Class);
		missingBase.CreateType();

		var builder = new PersistedAssemblyBuilder(new AssemblyName(assemblyName), typeof(object).Assembly);
		var module = builder.DefineDynamicModule(assemblyName);
		module.DefineType($"{assemblyName}.{loadableTypeName}", TypeAttributes.Public | TypeAttributes.Class).CreateType();
		module.DefineType($"{assemblyName}.{brokenTypeName}", TypeAttributes.Public | TypeAttributes.Class, missingBase).CreateType();

		return Load(context, builder);
	}

	private static Assembly Load(AssemblyLoadContext context, PersistedAssemblyBuilder builder)
	{
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
