namespace Uno.Extensions.Navigation;

public static class ServiceProviderExtensions
{
	/// <summary>
	/// Attached the IServiceProvider instance to the <paramref name="window"/> and
	/// registers the Window for use in Navigation
	/// </summary>
	/// <param name="window">The Window to attach the IServiceProvider to</param>
	/// <param name="services">The IServiceProvider instance to attach</param>
	public static void AttachServices(this Window window, IServiceProvider services)
	{
		window.Content
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

	internal static IServiceProvider CloneScopedInstance<T>(this IServiceProvider target, IServiceProvider source ) where T : notnull
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
		provider.GetRequiredService<TRepository>().AddInstance(serviceType,instance);
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
		var singleton = provider.GetRequiredService<TRepository>().GetInstance<T>();
		if (singleton is T singletonOfT)
		{
			return singletonOfT;
		}
		return default;
	}

	private static T? GetInstance<T>(this IInstanceRepository repository)
	{
		var value = repository.Instances.TryGetValue(typeof(T), out var repoValue) ? repoValue : null;
		if (value is Func<T> valueCreator)
		{
			var instance = valueCreator();
			if (instance is T instanceOfT)
			{
				repository.AddInstance(typeof(T),instanceOfT);
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

		window.AttachServices(services);

		root.Host(initialRoute, initialView, initialViewModel);

		return root;
	}
}
