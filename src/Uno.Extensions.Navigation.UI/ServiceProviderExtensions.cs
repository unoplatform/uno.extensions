namespace Uno.Extensions.Navigation;

public static class ServiceProviderExtensions
{
	public static void AttachServices(this Window window, IServiceProvider services)
	{
		window.Content
				.AttachServiceProvider(services)
				.RegisterWindow(window);
	}

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
