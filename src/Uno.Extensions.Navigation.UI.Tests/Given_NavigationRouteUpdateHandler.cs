using System;
using System.Collections.Generic;
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
public partial class Given_NavigationRouteUpdateHandler
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

	[TestMethod]
	public void When_CollectUpdatedViewModels_WithNullTypes_Then_Empty()
	{
		var resolver = CreateResolver();

		NavigationRouteUpdateHandler.CollectUpdatedViewModels(null, resolver).Should().BeEmpty(
			"an unknown delta must refresh nothing — conservative default");
	}

	[TestMethod]
	public void When_CollectUpdatedViewModels_WithUnregisteredType_Then_Empty()
	{
		var resolver = CreateResolver();

		NavigationRouteUpdateHandler.CollectUpdatedViewModels([typeof(GeneratedXamlPartial)], resolver).Should().BeEmpty();
	}

	[TestMethod]
	public void When_CollectUpdatedViewModels_WithRegisteredViewModel_Then_ContainsViewModel()
	{
		var resolver = CreateResolver();

		NavigationRouteUpdateHandler.CollectUpdatedViewModels([typeof(RegisteredViewModel)], resolver)
			.Should().BeEquivalentTo(new[] { typeof(RegisteredViewModel) });
	}

	[TestMethod]
	public void When_CollectUpdatedViewModels_WithRegisteredViewOnly_Then_Empty()
	{
		var resolver = CreateResolver();

		// Updated views are owned by Uno's element-update walk; a view-only delta must not
		// trigger a view-model rebuild (it would discard un-persisted view-model state on
		// every page edit — the behavior Given_FrameContentRehook pins).
		NavigationRouteUpdateHandler.CollectUpdatedViewModels([typeof(RegisteredPage)], resolver).Should().BeEmpty();
	}

	[TestMethod]
	public void When_CollectUpdatedViewModels_WithMappedModel_Then_ContainsBindableViewModel()
	{
		var services = new ServiceCollection();
		var views = new MappedViewRegistry(services, new Dictionary<Type, Type>
		{
			[typeof(MappedModel)] = typeof(BindableMappedModel),
		});
		var routes = new RouteRegistry(services);
		views.Register(new ViewMap<RegisteredPage, MappedModel>());
		routes.Register(new RouteMap("Mapped", View: views.FindByViewModel<MappedModel>()));
		var resolver = new MappedRouteResolver(NullLogger<MappedRouteResolver>.Instance, routes, views);

		NavigationRouteUpdateHandler.CollectUpdatedViewModels([typeof(MappedModel)], resolver)
			.Should().BeEquivalentTo(new[] { typeof(BindableMappedModel) },
				"the delta contains the model type, but the route table holds the mapped " +
				"(bindable) view model — the MVUX shape from #3142");
	}

	[TestMethod]
	public void When_CollectUpdatedViewModels_WithViewTypeOnDefaultResolver_Then_EmptyAndRouteTableIntact()
	{
		var services = new ServiceCollection();
		var views = new ViewRegistry(services);
		var routes = new RouteRegistry(services);
		var viewMap = new ViewMap<FrameworkElementPage>();
		views.Register(viewMap);
		// Route registered at the view type's full name — the same shape as the TabBar HR
		// tests, where RouteResolverDefault's convention fallback derives that exact path
		// from the type name on a FindByViewModel miss and would REPLACE the real mapping.
		routes.Register(new RouteMap("FrameworkElementPage", View: viewMap));
		var resolver = new RouteResolverDefault(NullLogger<RouteResolverDefault>.Instance, routes, views);

		var collected = NavigationRouteUpdateHandler.CollectUpdatedViewModels([typeof(FrameworkElementPage)], resolver);

		collected.Should().BeEmpty(
			"view types are owned by the element-update walk and must not trigger view-model refreshes");
		var mapping = resolver.FindByPath("FrameworkElementPage");
		mapping.Should().NotBeNull();
		mapping!.RenderView.Should().Be(typeof(FrameworkElementPage),
			"the lookup must not let the convention fallback replace the registered mapping");
		mapping.ViewModel.Should().BeNull(
			"the registered route has no view model; a view-typed ViewModel here is route-table corruption");
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

	private sealed partial class FrameworkElementPage : Microsoft.UI.Xaml.FrameworkElement
	{
	}

	private sealed class MappedModel
	{
	}

	private sealed class BindableMappedModel
	{
	}
}
