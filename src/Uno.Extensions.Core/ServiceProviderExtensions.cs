namespace Uno.Extensions;

public static class ServiceProviderExtensions
{
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
}
