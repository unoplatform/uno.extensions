using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests;

[TestClass]
public class Given_NavigationRouteUpdateHandler
{
	[TestMethod]
	public void When_Unregister_Then_ContextLiveTreeReferencesCleared()
	{
		var resolver = CreateResolver();
		var context = new NavigationRouteContext
		{
			RouteBuilder = (_, _) => { },
			Views = new ViewRegistry(new ServiceCollection()),
			Routes = new RouteRegistry(new ServiceCollection()),
			Resolver = resolver,
			RootRegion = new FakeRegion(),
		};

		NavigationRouteUpdateHandler.Register(context);
		NavigationRouteUpdateHandler.Unregister(context);

		context.Resolver.Should().BeNull("Unregister must drop the resolver so the route tree is released");
		context.RootRegion.Should().BeNull("Unregister must drop the live region tree reference");
	}

	[TestMethod]
	public void When_RootRegionSet_Then_ItIsHeldWeakly()
	{
		var context = new NavigationRouteContext
		{
			RouteBuilder = (_, _) => { },
			Views = new ViewRegistry(new ServiceCollection()),
			Routes = new RouteRegistry(new ServiceCollection()),
		};

		var reference = AssignRootRegion(context);

		for (var i = 0; i < 10 && reference.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		reference.IsAlive.Should().BeFalse("RootRegion must be held weakly so it does not pin the region tree");
		context.RootRegion.Should().BeNull("a collected root region reads back as null");
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static WeakReference AssignRootRegion(NavigationRouteContext context)
	{
		var region = new FakeRegion();
		context.RootRegion = region;
		return new WeakReference(region, trackResurrection: false);
	}

	private sealed class FakeRegion : IRegion
	{
		public string? Name => null;
		public Microsoft.UI.Xaml.FrameworkElement? View => null;
		public IServiceProvider? Services => null;
		public IRegion? Parent => null;
		public System.Collections.Generic.ICollection<IRegion> Children { get; } = new System.Collections.Generic.List<IRegion>();
		public void ReassignParent() { }
		public void Detach() { }
	}

	[TestMethod]
	public void When_UpdatedTypesAreNull_Then_CascadeIsAllowed()
	{
		var resolver = CreateResolver();

		var shouldCascade = NavigationRouteUpdateHandler.ShouldCascadeForUpdatedTypes(null, resolver);

		shouldCascade.Should().BeTrue();
	}

	[TestMethod]
	public void When_UpdatedTypeIsNotNavigationRegistered_Then_CascadeIsSkipped()
	{
		var resolver = CreateResolver();

		var shouldCascade = NavigationRouteUpdateHandler.ShouldCascadeForUpdatedTypes([typeof(GeneratedXamlPartial)], resolver);

		shouldCascade.Should().BeFalse();
	}

	[TestMethod]
	public void When_UpdatedTypeIsRegisteredView_Then_CascadeIsAllowed()
	{
		var resolver = CreateResolver();

		var shouldCascade = NavigationRouteUpdateHandler.ShouldCascadeForUpdatedTypes([typeof(RegisteredPage)], resolver);

		shouldCascade.Should().BeTrue();
	}

	[TestMethod]
	public void When_UpdatedTypeIsRegisteredViewModel_Then_CascadeIsAllowed()
	{
		var resolver = CreateResolver();

		var shouldCascade = NavigationRouteUpdateHandler.ShouldCascadeForUpdatedTypes([typeof(RegisteredViewModel)], resolver);

		shouldCascade.Should().BeTrue();
	}

	private static RouteResolver CreateResolver()
	{
		var services = new ServiceCollection();
		var views = new ViewRegistry(services);
		var routes = new RouteRegistry(services);
		var registeredView = new ViewMap<RegisteredPage, RegisteredViewModel>();

		views.Register(registeredView);
		routes.Register(new RouteMap("Registered", View: registeredView));

		return new RouteResolver(NullLogger<RouteResolver>.Instance, routes, views);
	}

	private sealed class RegisteredPage
	{
	}

	private sealed class RegisteredViewModel
	{
	}

	private sealed class GeneratedXamlPartial
	{
	}
}
