using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests;

[TestClass]
public class Given_NavigationRouteUpdateHandler
{
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

	// RouteResolverDefault (the resolver apps actually get) has an implicit-mapping fallback that
	// must not fool the cascade relevance check into matching arbitrary types (studio.live#2716).
	[TestMethod]
	public void When_UpdatedTypeIsNotNavigationRegistered_WithDefaultResolver_Then_CascadeIsSkipped()
	{
		var resolver = CreateDefaultResolver();

		var shouldCascade = NavigationRouteUpdateHandler.ShouldCascadeForUpdatedTypes([typeof(GeneratedXamlPartial)], resolver);

		shouldCascade.Should().BeFalse();
	}

	[TestMethod]
	public void When_UpdatedTypeIsRegisteredView_WithDefaultResolver_Then_CascadeIsAllowed()
	{
		var resolver = CreateDefaultResolver();

		var shouldCascade = NavigationRouteUpdateHandler.ShouldCascadeForUpdatedTypes([typeof(RegisteredPage)], resolver);

		shouldCascade.Should().BeTrue();
	}

	[TestMethod]
	public void When_UpdatedTypeIsRegisteredViewModel_WithDefaultResolver_Then_CascadeIsAllowed()
	{
		var resolver = CreateDefaultResolver();

		var shouldCascade = NavigationRouteUpdateHandler.ShouldCascadeForUpdatedTypes([typeof(RegisteredViewModel)], resolver);

		shouldCascade.Should().BeTrue();
	}

	[TestMethod]
	public void When_UpdatedTypeIsNotNavigationRegistered_WithDefaultResolver_Then_MappingsAreNotMutated()
	{
		var resolver = CreateDefaultResolver();
		var mappingCountBefore = resolver.MappingCount;

		NavigationRouteUpdateHandler.ShouldCascadeForUpdatedTypes([typeof(GeneratedXamlPartial)], resolver);

		resolver.MappingCount.Should().Be(mappingCountBefore,
			"the hot-reload relevance check must be side-effect free and not fabricate implicit mappings for updated types");
	}

	// The cascade gate also fires on route-table changes — how a route registered behind a
	// hot-reload-flipped flag cascades even though the flag's declaring type is not registered.
	[TestMethod]
	public void When_RouteAddedThenRebuilt_Then_MappingsSignatureChanges()
	{
		var (routes, views) = CreateRegistries();
		var resolver = new RouteResolver(NullLogger<RouteResolver>.Instance, routes, views);
		var signatureBefore = resolver.GetMappingsSignature();

		routes.Register(new RouteMap("AddedByHotReload"));
		resolver.Rebuild();

		resolver.GetMappingsSignature().SetEquals(signatureBefore).Should().BeFalse(
			"adding a route and rebuilding must change the mappings signature so the hot-reload cascade fires");
	}

	[TestMethod]
	public void When_RebuiltWithoutChanges_Then_MappingsSignatureUnchanged()
	{
		var (routes, views) = CreateRegistries();
		var resolver = new RouteResolver(NullLogger<RouteResolver>.Instance, routes, views);
		var signatureBefore = resolver.GetMappingsSignature();

		resolver.Rebuild();

		resolver.GetMappingsSignature().SetEquals(signatureBefore).Should().BeTrue(
			"rebuilding from unchanged registries must produce the same signature so no spurious cascade fires");
	}

	private static RouteResolver CreateResolver()
	{
		var (routes, views) = CreateRegistries();
		return new RouteResolver(NullLogger<RouteResolver>.Instance, routes, views);
	}

	private static InspectableRouteResolverDefault CreateDefaultResolver()
	{
		var (routes, views) = CreateRegistries();
		return new InspectableRouteResolverDefault(routes, views);
	}

	private static (IRouteRegistry Routes, IViewRegistry Views) CreateRegistries()
	{
		var services = new ServiceCollection();
		var views = new ViewRegistry(services);
		var routes = new RouteRegistry(services);
		var registeredView = new ViewMap<RegisteredPage, RegisteredViewModel>();

		views.Register(registeredView);
		routes.Register(new RouteMap("Registered", View: registeredView));

		return (routes, views);
	}

	private sealed class InspectableRouteResolverDefault(IRouteRegistry routes, IViewRegistry views)
		: RouteResolverDefault(NullLogger<RouteResolverDefault>.Instance, routes, views)
	{
		public int MappingCount => Mappings.Count;
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
