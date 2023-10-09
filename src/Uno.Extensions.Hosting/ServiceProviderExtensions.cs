using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions;

internal static class ServiceProviderExtensions
{
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
}
