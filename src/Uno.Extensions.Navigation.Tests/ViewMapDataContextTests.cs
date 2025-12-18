namespace Uno.Extensions.Navigation.Tests;

/// <summary>
/// Tests for ViewMap DataContext initialization timing issues
/// Reproduces: https://github.com/unoplatform/uno.extensions/issues/XXXX
/// Bug: DataContext is null on first launch but works after Hot Reload
/// </summary>
[TestClass]
public class ViewMapDataContextTests
{
	/// <summary>
	/// Test that ViewMap registration properly registers ViewModel types in DI container
	/// immediately when ViewMap.RegisterTypes is called
	/// </summary>
	[TestMethod]
	public void ViewMap_RegisterTypes_ShouldRegisterViewModelInDI()
	{
		// Arrange
		var services = new ServiceCollection();
		var viewMap = new ViewMap<TestPage, TestViewModel>();

		// Act
		viewMap.RegisterTypes(services);
		var provider = services.BuildServiceProvider();

		// Assert - ViewModel should be resolvable immediately after RegisterTypes
		var viewModel = provider.GetService<TestViewModel>();
		viewModel.Should().NotBeNull("ViewModel should be registered in DI container when ViewMap.RegisterTypes is called");
	}

	/// <summary>
	/// Test that ViewRegistry.InsertItem calls ViewMap.RegisterTypes
	/// This is critical for the timing issue - ViewModels must be registered when ViewMaps are inserted
	/// </summary>
	[TestMethod]
	public void ViewRegistry_InsertItem_ShouldCallRegisterTypes()
	{
		// Arrange
		var services = new ServiceCollection();
		var viewRegistry = new ViewRegistry(services);

		// Act - This should trigger InsertItem which should call RegisterTypes
		viewRegistry.Register(
			new ViewMap<TestPage, TestViewModel>()
		);

		var provider = services.BuildServiceProvider();

		// Assert - ViewModel should be registered
		var viewModel = provider.GetService<TestViewModel>();
		viewModel.Should().NotBeNull("ViewModel should be registered when ViewMap is registered in ViewRegistry");
	}

	/// <summary>
	/// Test that multiple ViewMap registrations all have their ViewModels registered
	/// </summary>
	[TestMethod]
	public void ViewRegistry_MultipleViewMaps_AllViewModelsShouldBeRegistered()
	{
		// Arrange
		var services = new ServiceCollection();
		var viewRegistry = new ViewRegistry(services);

		// Act - Register multiple ViewMaps
		viewRegistry.Register(
			new ViewMap<TestPage, TestViewModel>(),
			new ViewMap<TestPage2, TestViewModel2>(),
			new ViewMap<TestPage3, TestViewModel3>()
		);

		var provider = services.BuildServiceProvider();

		// Assert - All ViewModels should be resolvable
		provider.GetService<TestViewModel>().Should().NotBeNull();
		provider.GetService<TestViewModel2>().Should().NotBeNull();
		provider.GetService<TestViewModel3>().Should().NotBeNull();
	}

	/// <summary>
	/// Test that ViewMap registration happens before any RouteMap is used
	/// This ensures that ViewModels are available when routes are resolved
	/// </summary>
	[TestMethod]
	public void ViewRegistry_And_RouteRegistry_Registration_ViewModelsShouldBeAvailableImmediately()
	{
		// Arrange
		var services = new ServiceCollection();
		var viewRegistry = new ViewRegistry(services);
		var routeRegistry = new RouteRegistry(services);

		// Act - Register ViewMaps and RouteMaps
		viewRegistry.Register(
			new ViewMap<TestPage, TestViewModel>()
		);

		routeRegistry.Register(
			new RouteMap("TestPage", View: new ViewMap<TestPage, TestViewModel>())
		);

		var provider = services.BuildServiceProvider();

		// Assert - ViewModel should be available
		var viewModel = provider.GetService<TestViewModel>();
		viewModel.Should().NotBeNull("ViewModel should be registered and available");
	}

	/// <summary>
	/// Test MappedViewMap (used with MVUX) properly registers both original and mapped ViewModels
	/// </summary>
	[TestMethod]
	public void MappedViewMap_RegisterTypes_ShouldRegisterBothViewModels()
	{
		// Arrange
		var services = new ServiceCollection();
		var mappedViewMap = new MappedViewMap(
			View: typeof(TestPage),
			ViewModel: typeof(TestViewModel),
			MappedViewModel: typeof(TestBindableViewModel)
		);

		// Act
		mappedViewMap.RegisterTypes(services);
		var provider = services.BuildServiceProvider();

		// Assert - Both ViewModels should be registered
		var originalViewModel = provider.GetService<TestViewModel>();
		var mappedViewModel = provider.GetService<TestBindableViewModel>();
		
		originalViewModel.Should().NotBeNull("Original ViewModel should be registered");
		mappedViewModel.Should().NotBeNull("Mapped ViewModel should be registered");
	}

	/// <summary>
	/// Test MappedViewRegistry properly creates MappedViewMap and registers both ViewModels
	/// </summary>
	[TestMethod]
	public void MappedViewRegistry_InsertItem_ShouldRegisterBothViewModels()
	{
		// Arrange
		var services = new ServiceCollection();
		var viewModelMappings = new Dictionary<Type, Type>
		{
			{ typeof(TestViewModel), typeof(TestBindableViewModel) }
		};
		var viewRegistry = new MappedViewRegistry(services, viewModelMappings);

		// Act - Register ViewMap - this should create a MappedViewMap internally
		viewRegistry.Register(
			new ViewMap<TestPage, TestViewModel>()
		);

		var provider = services.BuildServiceProvider();

		// Assert - Both ViewModels should be registered
		var originalViewModel = provider.GetService<TestViewModel>();
		var mappedViewModel = provider.GetService<TestBindableViewModel>();
		
		originalViewModel.Should().NotBeNull("Original ViewModel should be registered");
		mappedViewModel.Should().NotBeNull("Mapped ViewModel (from MappedViewMap) should be registered");
	}

	/// <summary>
	/// Test that View type is also registered when using ViewMap with generic type
	/// </summary>
	[TestMethod]
	public void ViewMap_Generic_ShouldRegisterViewType()
	{
		// Arrange
		var services = new ServiceCollection();
		var viewMap = new ViewMap<TestPage, TestViewModel>();

		// Act
		viewMap.RegisterTypes(services);
		var provider = services.BuildServiceProvider();

		// Assert - Both View and ViewModel should be registered
		var view = provider.GetService<TestPage>();
		var viewModel = provider.GetService<TestViewModel>();
		
		view.Should().NotBeNull("View should be registered");
		viewModel.Should().NotBeNull("ViewModel should be registered");
	}

	/// <summary>
	/// Critical test: Simulate the actual UseNavigation registration flow
	/// to ensure ViewModels are accessible from scoped service providers
	/// </summary>
	[TestMethod]
	public void SimulateUseNavigation_ViewModelShouldBeAccessibleFromScopedProvider()
	{
		// Arrange - Simulate the exact flow from ServiceCollectionExtensions.AddNavigation
		var services = new ServiceCollection();
		
		// Step 1: Create registries (like AddNavigation does)
		var views = new ViewRegistry(services);
		var routes = new RouteRegistry(services);
		
		// Step 2: Register ViewMaps (simulating routeBuilder callback)
		views.Register(
			new ViewMap<TestPage, TestViewModel>()
		);
		routes.Register(
			new RouteMap("TestPage", View: new ViewMap<TestPage, TestViewModel>())
		);
		
		// Step 3: Register registries as singletons (like AddNavigation does)
		services.AddSingleton(views.GetType(), views);
		services.AddSingleton<IViewRegistry>(sp => (IViewRegistry)sp.GetRequiredService(views.GetType()));
		services.AddSingleton(routes.GetType(), routes);
		services.AddSingleton<IRouteRegistry>(sp => (RouteRegistry)sp.GetRequiredService(routes.GetType()));
		
		// Step 4: Build service provider
		var rootProvider = services.BuildServiceProvider();
		
		// Step 5: Create a scoped provider (like navigation does during page navigation)
		using var scope = rootProvider.CreateScope();
		var scopedProvider = scope.ServiceProvider;
		
		// Assert - ViewModel should be accessible from scoped provider
		var viewModelFromRoot = rootProvider.GetService<TestViewModel>();
		var viewModelFromScoped = scopedProvider.GetService<TestViewModel>();
		
		viewModelFromRoot.Should().NotBeNull("ViewModel should be accessible from root provider");
		viewModelFromScoped.Should().NotBeNull("ViewModel should be accessible from scoped provider (CRITICAL for navigation)");
	}

	/// <summary>
	/// Test that transient ViewModels registered via AddTransient(Type) 
	/// are properly resolved from scoped service providers
	/// </summary>
	[TestMethod]
	public void TransientRegistration_ByType_ShouldResolveFromScopedProvider()
	{
		// Arrange
		var services = new ServiceCollection();
		
		// This mimics what ViewMap.RegisterTypes does on line 21
		services.AddTransient(typeof(TestViewModel));
		
		var rootProvider = services.BuildServiceProvider();
		
		// Act - Create scoped provider and try to resolve
		using var scope = rootProvider.CreateScope();
		var scopedProvider = scope.ServiceProvider;
		
		var viewModelFromScoped = scopedProvider.GetService(typeof(TestViewModel));
		
		// Assert
		viewModelFromScoped.Should().NotBeNull("Transient service registered by Type should resolve from scoped provider");
		viewModelFromScoped.Should().BeOfType<TestViewModel>();
	}

	/// <summary>
	/// CRITICAL TEST: Reproduces the bug!
	/// When ViewMap is registered only in RouteMap (not in ViewRegistry), 
	/// the ViewModel is NOT registered in DI because RouteRegistry.InsertItem 
	/// doesn't call ViewMap.RegisterTypes()
	/// </summary>
	[TestMethod]
	public void RouteRegistry_InsertItem_ShouldRegisterViewMapTypes_BugReproduction()
	{
		// Arrange
		var services = new ServiceCollection();
		var routeRegistry = new RouteRegistry(services);

		// Act - Register a RouteMap with an embedded ViewMap
		// This is what users do in their app setup
		routeRegistry.Register(
			new RouteMap("TestPage", View: new ViewMap<TestPage, TestViewModel>())
		);

		var provider = services.BuildServiceProvider();

		// Assert - ViewModel should be registered BUT IT'S NOT!
		var viewModel = provider.GetService<TestViewModel>();
		
		// THIS TEST WILL FAIL - revealing the bug!
		viewModel.Should().NotBeNull("ViewModel should be registered when ViewMap is in RouteMap");
	}

	// Test helper classes
	public class TestPage
	{
		public TestPage() { }
	}

	public class TestPage2
	{
		public TestPage2() { }
	}

	public class TestPage3
	{
		public TestPage3() { }
	}

	public class TestViewModel
	{
		public string Name { get; set; } = "Test";
	}

	public class TestViewModel2
	{
		public string Name { get; set; } = "Test2";
	}

	public class TestViewModel3
	{
		public string Name { get; set; } = "Test3";
	}

	public class TestBindableViewModel
	{
		public string Name { get; set; } = "BindableTest";
	}
}
