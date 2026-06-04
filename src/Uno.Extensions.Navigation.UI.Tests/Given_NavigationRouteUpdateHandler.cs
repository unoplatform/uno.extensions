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
