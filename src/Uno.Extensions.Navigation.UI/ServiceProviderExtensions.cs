namespace Uno.Extensions.Navigation;

public static class ServiceProviderExtensions
{
	public static void AttachServices(this Window window, IServiceProvider services)
	{
		window.Content
				.AttachServiceProvider(services)
				.RegisterWindow(window);
	}

	internal static IServiceProvider RegisterWindow(this IServiceProvider services, Window window)
	{
		var provider = services.GetRequiredService<IWindowProvider>();
		provider.Current = window;
		return services;
	}

	internal static IServiceProvider CreateNavigationScope(this IServiceProvider services)
	{
		var scoped = services.CreateScope().ServiceProvider;
		scoped.GetRequiredService<IWindowProvider>().Current = services.GetRequiredService<IWindowProvider>().Current;
		return scoped;
	}
	public static IServiceProvider AddInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
	{
		return provider.AddInstance(typeof(T), instanceCreator);
	}

	public static IServiceProvider AddInstance<T>(this IServiceProvider provider, Type serviceType, Func<T> instanceCreator)
	{
		provider.GetRequiredService<IInstanceRepository>().Instances[serviceType] = instanceCreator;
		return provider;
	}

	public static IServiceProvider AddInstance<T>(this IServiceProvider provider, T instance)
	{
		return provider.AddInstance(typeof(T), instance!);
	}

	public static IServiceProvider AddInstance(this IServiceProvider provider, Type serviceType, object instance)
	{
		provider.GetRequiredService<IInstanceRepository>().Instances[serviceType] = instance;
		return provider;
	}

	public static T? GetInstance<T>(this IServiceProvider provider)
	{
		var value = provider.GetInstance(typeof(T));
		if (value is Func<T> valueCreator)
		{
			var instance = valueCreator();
			provider.AddInstance(instance);
			return instance;
		}

		if (value is T typedValue)
		{
			return typedValue;
		}

		return default;
	}

	public static object? GetInstance(this IServiceProvider provider, Type type)
	{
		return provider.GetRequiredService<IInstanceRepository>().Instances.TryGetValue(type, out var value) ? value : null;
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
