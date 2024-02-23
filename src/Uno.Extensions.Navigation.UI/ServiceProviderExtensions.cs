namespace Uno.Extensions;

public static class ServiceProviderExtensions
{
	/// <summary>
	/// Attached the IServiceProvider instance to the <paramref name="window"/> and
	/// registers the Window for use in Navigation
	/// </summary>
	/// <param name="window">The Window to attach the IServiceProvider to</param>
	/// <param name="services">The IServiceProvider instance to attach</param>
	public async static Task<IServiceProvider> AttachServicesAsync(this Window window, IServiceProvider services)
	{
		return await window.Content!
				.AttachServiceProvider(services)
				.RegisterWindowAsync(window);
	}

	/// <summary>
	/// Registers the Window with the specified IServiceProvider instance
	/// </summary>
	/// <param name="services">The IServiceProvider to register the Window with</param>
	/// <param name="window">The Window to be registered with the IServiceProvider instance</param>
	/// <returns>The IServiceProvider instance (for fluent calling of other methods)</returns>
	public async static Task<IServiceProvider> RegisterWindowAsync(this IServiceProvider services, Window window)
	{

		services = services.AddScopedInstance(window)
						.AddScopedInstance<IDispatcher>(new Dispatcher(window));

		// Initialization is done after adding both Window and IDispatcher to the scoped container
		// this way if any initializer relies on IDispatcher, it can be retrieved.
		var initializers = services.GetServices<IWindowInitializer>();
		foreach (var init in initializers)
		{
			await init.InitializeWindowAsync(window);
		}

		return services;
	}

	internal static IServiceProvider CreateNavigationScope(this IServiceProvider services)
	{
		var scoped = services.CreateScope().ServiceProvider;
		return scoped.CloneScopedInstance<Window>(services)
					.CloneScopedInstance<IDispatcher>(services);
	}

	

	/// <summary>
	/// Initializes navigation for an application using a ContentControl
	/// </summary>
	/// <param name="window">The application Window to initialize navigation for</param>
	/// <param name="buildHost">Function to create IHost</param>
	/// <param name="navigationRoot">[optional] Where to host app navigation (only required for nesting navigation in an existing application)</param>
	/// <param name="initialRoute">[optional] Initial navigation route</param>
	/// <param name="initialView">[optional] Initial navigation view</param>
	/// <param name="initialViewModel">[optional] Initial navigation viewmodel</param>
	/// <param name="initialNavigate">[optional] Callback to drive initial navigation for app</param>
	/// <returns>The created IHost</returns>
	public static Task<IHost> InitializeNavigationAsync(
		this Window window,
		Func<Task<IHost>> buildHost,
		ContentControl? navigationRoot = null,
		string? initialRoute = "",
		Type? initialView = null,
		Type? initialViewModel = null,
		Func<IServiceProvider, INavigator, Task>? initialNavigate = null)
	{

		// Make sure we have a navigation root
		var root = navigationRoot ?? new ContentControl
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch
		};

		return window.InternalInitializeNavigationAsync(
			buildHost,
			root,
			initialRoute, initialView, initialViewModel, null, initialNavigate);
	}

	internal static Task<IHost> InternalInitializeNavigationAsync(
		this Window window,
		Func<Task<IHost>> buildHost,
		ContentControl navigationRoot,
		string? initialRoute = "",
		Type? initialView = null,
		Type? initialViewModel = null,
		Action<Window, FrameworkElement, Task>? initializeViewHost = null,
		Func<IServiceProvider, INavigator, Task>? initialNavigate = null)
	{
		if (window.Content is null)
		{
			window.Content = navigationRoot;
		}

		var buildTask = window.BuildAndInitializeHostAsync(navigationRoot, buildHost, initialRoute, initialView, initialViewModel, initialNavigate);
		initializeViewHost?.Invoke(window, navigationRoot, buildTask);
		return buildTask;
	}

	private static async Task<IHost> BuildAndInitializeHostAsync(
		this Window window,
		FrameworkElement viewHost,
		Func<Task<IHost>> buildHost,
		string? initialRoute = "", Type? initialView = null, Type? initialViewModel = null,
		Func<IServiceProvider, INavigator, Task>? initialNavigate = null)
	{
		// Force immediate return of Task to avoid synchronous execution of buildHost
		// It's important that buildHost is still executed on UI thread, so can't do
		// Task.Run to force background execution.
		await Task.Yield();

		var host = await buildHost();
		var services = await window.AttachServicesAsync(host.Services);
		var startup = viewHost.HostAsync(services, initialRoute, initialView, initialViewModel, initialNavigate);

		await Task.Run(() => host.StartAsync());

		await startup;

		// Fallback to make sure the window is activated
		window.Activate();

		return host;
	}
}
