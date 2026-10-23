using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Uno.Extensions.Navigation.Tests;

/// <summary>
/// Guards which assemblies <see cref="RouteResolverDefault"/> searches when it resolves a route by name.
/// </summary>
/// <remarks>
/// A downstream host can run several apps in one process, each in its own collectible <see cref="AssemblyLoadContext"/>.
/// The implicit lookup must only see the resolver's own load context and the default one, never another app's context,
/// which may be unloading.
/// </remarks>
[TestClass]
public class Given_RouteResolverDefault
{
	[TestMethod]
	public void When_TypeOnlyInOtherLoadContext_Then_NotResolved()
	{
		var otherContext = new AssemblyLoadContext("OtherLoadContext", isCollectible: true);
		try
		{
			LoadEmittedAssembly(otherContext, "OtherLoadContextApp", "OtherLoadContextOnlyModel");
			var resolver = CreateResolver(typeof(RouteResolverDefault));

			ViewModelForPath(resolver, "OtherLoadContextOnly").Should().BeNull(
				"a type loaded only into another app's load context must not be resolved implicitly");
			ViewModelForPath(resolver, "DefaultLoadContextOnly").Should().Be(typeof(DefaultLoadContextOnlyModel));
		}
		finally
		{
			otherContext.Unload();
		}
	}

	[TestMethod]
	public void When_ResolverInOwnLoadContext_Then_OwnTypeWinsOverDefault()
	{
		var appContext = new AssemblyLoadContext("AppLoadContext", isCollectible: true);
		try
		{
			var resolver = CreateResolverInOwnContext(appContext);
			var appAssembly = LoadEmittedAssembly(appContext, "OwnLoadContextApp", nameof(SharedNameModel));

			var sharedNameModel = ViewModelForPath(resolver, "SharedName");
			sharedNameModel.Should().NotBeNull();
			sharedNameModel!.Assembly.Should().BeSameAs(appAssembly,
				"the app's own type must win over a same-named type in the default load context");
			ViewModelForPath(resolver, "DefaultLoadContextOnly").Should().Be(typeof(DefaultLoadContextOnlyModel));
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
			var resolver = CreateResolverInOwnContext(appContext);
			LoadEmittedAssembly(AssemblyLoadContext.Default, "SameSimpleNameLib", "DefaultCopyOnlyModel");
			var ownCopy = LoadEmittedAssembly(appContext, "SameSimpleNameLib", "OwnCopyOnlyModel");

			ViewModelForPath(resolver, "DefaultCopyOnly").Should().BeNull(
				"the default context's copy of an assembly the resolver's own context also has must not be scanned");
			ViewModelForPath(resolver, "OwnCopyOnly").Should().Be(ownCopy.GetType("SameSimpleNameLib.OwnCopyOnlyModel", throwOnError: true));
		}
		finally
		{
			appContext.Unload();
		}
	}

	/// <summary>
	/// Loads a second copy of the resolver's assembly into <paramref name="context"/>; every other dependency falls back
	/// to the default context.
	/// </summary>
	private static IRouteResolver CreateResolverInOwnContext(AssemblyLoadContext context)
	{
		var resolverAssembly = context.LoadFromAssemblyPath(typeof(RouteResolverDefault).Assembly.Location);
		var resolverType = resolverAssembly.GetType(typeof(RouteResolverDefault).FullName!, throwOnError: true)!;
		return CreateResolver(resolverType);
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

	private static IRouteResolver CreateResolver(Type resolverType)
	{
		var services = new ServiceCollection();
		var logger = Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(resolverType));
		return (IRouteResolver)Activator.CreateInstance(resolverType, logger, new RouteRegistry(services), new ViewRegistry(services))!;
	}

	private static Type? ViewModelForPath(IRouteResolver resolver, string path)
	{
		var route = resolver.FindByPath(path);
		route.Should().NotBeNull($"an implicit mapping is always created for path '{path}'");
		return route!.ViewModel;
	}

	public class DefaultLoadContextOnlyModel
	{
	}

	public class SharedNameModel
	{
	}
}
