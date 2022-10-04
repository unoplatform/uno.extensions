using Uno.Extensions.Navigation;
using Windows.UI.Xaml;

namespace Uno.Extensions;

public static class ServiceProviderExtensions
{
	/// <summary>
	/// Attached the IServiceProvider instance to the <paramref name="window"/> and
	/// registers the Window for use in Navigation
	/// </summary>
	/// <param name="window">The Window to attach the IServiceProvider to</param>
	/// <param name="services">The IServiceProvider instance to attach</param>
	public static IServiceProvider AttachServices(this Window window, IServiceProvider services)
	{
		return window.Content
				.AttachServiceProvider(services)
				.RegisterWindow(window);
	}

	/// <summary>
	/// Registers the Window with the specified IServiceProvider instance
	/// </summary>
	/// <param name="services">The IServiceProvider to register the Window with</param>
	/// <param name="window">The Window to be registered with the IServiceProvider instance</param>
	/// <returns>The IServiceProvider instance (for fluent calling of other methods)</returns>
	public static IServiceProvider RegisterWindow(this IServiceProvider services, Window window)
	{
		return services.AddScopedInstance(window)
						.AddScopedInstance<IDispatcher>(new Dispatcher(window));
	}

	internal static IServiceProvider CreateNavigationScope(this IServiceProvider services)
	{
		var scoped = services.CreateScope().ServiceProvider;
		return scoped.CloneScopedInstance<Window>(services)
					.CloneScopedInstance<IDispatcher>(services);
	}

	internal static IServiceProvider CloneScopedInstance<T>(this IServiceProvider target, IServiceProvider source) where T : notnull
	{
		return target.AddScopedInstance(source.GetRequiredService<T>());
	}

	public static IServiceProvider AddScopedInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
	{
		return provider.AddScopedInstance(typeof(T), instanceCreator);
	}

	public static IServiceProvider AddScopedInstance<T>(this IServiceProvider provider, T instance)
	{
		return provider.AddScopedInstance(typeof(T), instance!);
	}

	public static IServiceProvider AddScopedInstance(this IServiceProvider provider, Type serviceType, object instance)
	{
		return provider.AddInstance<IScopedInstanceRepository>(serviceType, instance!);
	}

	public static IServiceProvider AddSingletonInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
	{
		return provider.AddSingletonInstance(typeof(T), instanceCreator);
	}

	public static IServiceProvider AddSingletonInstance<T>(this IServiceProvider provider, T instance)
	{
		return provider.AddSingletonInstance(typeof(T), instance!);
	}

	public static IServiceProvider AddSingletonInstance(this IServiceProvider provider, Type serviceType, object instance)
	{
		return provider.AddInstance<ISingletonInstanceRepository>(serviceType, instance!);
	}

	private static IServiceProvider AddInstance<TRepository>(this IServiceProvider provider, Type serviceType, object instance) where TRepository : IInstanceRepository
	{
		provider.GetRequiredService<TRepository>().AddInstance(serviceType, instance);
		return provider;
	}

	private static IInstanceRepository AddInstance(this IInstanceRepository repository, Type serviceType, object instance)
	{
		repository.Instances[serviceType] = instance;
		return repository;
	}


	public static T? GetInstance<T>(this IServiceProvider provider)
	{
		return provider.GetInstance<IScopedInstanceRepository, T>() ??
				provider.GetInstance<ISingletonInstanceRepository, T>();
	}


	private static T? GetInstance<TRepository, T>(this IServiceProvider provider)
		where TRepository : IInstanceRepository
	{
		var singleton = provider.GetRequiredService<TRepository>().GetRepositoryInstance<T>();
		if (singleton is T singletonOfT)
		{
			return singletonOfT;
		}
		return default;
	}

	private static T? GetRepositoryInstance<T>(this IInstanceRepository repository)
	{
		var value = repository.Instances.TryGetValue(typeof(T), out var repoValue) ? repoValue : null;
		if (value is Func<T> valueCreator)
		{
			var instance = valueCreator();
			if (instance is T instanceOfT)
			{
				repository.AddInstance(typeof(T), instanceOfT);
			}
			return instance;
		}

		if (value is T typedValue)
		{
			return typedValue;
		}

		return default;
	}

	public static FrameworkElement AttachNavigation(this Window window, IServiceProvider services, string? initialRoute = "", Type? initialView = null, Type? initialViewModel = null)
	{
		var root = new ContentControl
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch
		};
		window.Content = root;
		services = window.AttachServices(services);
		root.Host(services, initialRoute, initialView, initialViewModel);

		return root;
	}

	/// <summary>
	/// Initializes navigation for an application using a ContentControl
	/// </summary>
	/// <param name="window">The application Window to initialize navigation for</param>
	/// <param name="buildHost">Function to create IHost</param>
	/// <param name="initialRoute">[optional] Initial navigation route</param>
	/// <param name="initialView">[optional] Initial navigation view</param>
	/// <param name="initialViewModel">[optional] Initial navigation viewmodel</param>
	/// <param name="navigationRoot">[optional] Where to host app navigation (only required for nesting navigation in an existing application)</param>
	/// <returns>The created IHost</returns>
	public static Task<IHost> InitializeNavigation(this Window window, Func<IHost> buildHost, string? initialRoute = "", Type? initialView = null, Type? initialViewModel = null, ContentControl? navigationRoot = null)
	{
		return window.InitializeNavigation<DefaultViewHostProvider>(buildHost, initialRoute, initialView, initialViewModel, navigationRoot);
	}

	internal static Task<IHost> InitializeNavigation<TViewHostProvider>(this Window window, Func<IHost> buildHost, string? initialRoute = "", Type? initialView = null, Type? initialViewModel = null, ContentControl? navigationRoot = null)
	  where TViewHostProvider : IViewHostProvider, new()
	{
		var viewHost = new TViewHostProvider();
		var root = viewHost.CreateViewHost();
		if (navigationRoot is null)
		{
			window.Content = root;
			window.Activate();
		}
		else
		{
			navigationRoot.Content = root;
		}

		IDeferrable? startupDeferral = null;

		var buildTask = window.BuildAndInitializeHost(root, buildHost, () => startupDeferral!, initialRoute, initialView, initialViewModel);
		startupDeferral = viewHost.InitializeViewHost(root, buildTask);
		return buildTask;
	}

	private static async Task<IHost> BuildAndInitializeHost(this Window window, FrameworkElement viewHost, Func<IHost> buildHost, Func<IDeferrable> startupDeferral, string? initialRoute = "", Type? initialView = null, Type? initialViewModel = null)
	{
		// Force immediate return of Task to avoid synchronous execution of buildHost
		// It's important that buildHost is still executed on UI thread, so can't do
		// Task.Run to force background execution.
		await Task.Yield();

		var host = buildHost();
		var splash = host.Services.GetRequiredService<SplashScreen>();
		splash.DeferralSource = startupDeferral();

		var services = window.AttachServices(host.Services);
		var startup = viewHost.Host(services, initialRoute, initialView, initialViewModel);

		await Task.Run(() => host.StartAsync());

		await startup;

		return host;
	}
}
