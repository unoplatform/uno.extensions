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
}
