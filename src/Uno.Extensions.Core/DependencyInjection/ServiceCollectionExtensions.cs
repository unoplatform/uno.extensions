using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions;

public static class ServiceCollectionExtensions
{
	private const string DefaultInstanceKey = "Default";

	public static IServiceCollection AddNamedSingleton<TService, TImplementation>(this IServiceCollection services, string Name, Func<IServiceProvider, TImplementation> implementationFactory)
	where TService : class where TImplementation : class, TService
	{
		// Register the concrete type (the NamedInstance will use this
		// rather than iterating through all the registered implementations
		// of TService
		services.TryAddTransient(implementationFactory);
		return services.AddNamedSingletonImplementation<TService, TImplementation>(Name);

	}

	public static IServiceCollection AddNamedSingleton<
		TService,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation
	>(this IServiceCollection services, string Name)
		where TService : class where TImplementation : class, TService
	{
		// Register the concrete type (the NamedInstance will use this
		// rather than iterating through all the registered implementations
		// of TService
		services.TryAddTransient<TImplementation>();
		return services.AddNamedSingletonImplementation<TService, TImplementation>(Name);
	}


	private static IServiceCollection AddNamedSingletonImplementation<TService, TImplementation>(this IServiceCollection services, string Name)
		where TService : class where TImplementation : class, TService
	{
		// Register for TService
		services.TryAddTransient<TService>(sp => sp.GetRequiredService<TImplementation>());
		return services
				// Register the named resolve
				.AddSingleton<INamedInstance<TService>>(sp=>new NamedInstance<TService,TImplementation>(sp,Name));
	}

	private static IServiceCollection AddNamedSingletonReference<TService>(this IServiceCollection services, string OriginalName, string Name)
	{
		return services
				// Register the named resolve
				.AddSingleton<INamedInstance<TService>>(sp => new NamedInstanceReference<TService>(sp, OriginalName, Name));
	}

	public static IServiceCollection SetDefaultInstance<
		TService,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation
	>(this IServiceCollection services)
		where TService : class where TImplementation : class, TService
	{
		return services.AddNamedSingleton<TService, TImplementation>(DefaultInstanceKey);
	}

	public static IServiceCollection SetDefaultInstance<TService> (this IServiceCollection services, string ExistingInstance)
	{
		return services.AddNamedSingletonReference<TService>(ExistingInstance, DefaultInstanceKey);
	}

	public static TService? GetDefaultInstance<TService>(this IServiceProvider sp)
	{
		return GetNamedService<TService>(sp, DefaultInstanceKey);
	}

	public static TService GetRequiredDefaultInstance<TService>(this IServiceProvider sp)
	{
		return GetRequiredNamedService<TService>(sp, DefaultInstanceKey);
	}


	private static INamedInstance<TService> FindNamedInstance<TService>(this IServiceProvider sp, string Name)
	{
		return sp.GetServices<INamedInstance<TService>>().First(x => x.Name == Name);
	}

	public static TService? GetNamedService<TService>(this IServiceProvider sp, string Name)
	{
		var resolver = sp.FindNamedInstance<TService>(Name);
		return resolver.Get();
	}

	public static TService GetRequiredNamedService<TService>(this IServiceProvider sp, string Name)
	{
		var resolver = sp.FindNamedInstance<TService>(Name);
		return resolver.GetRequired();
	}
}
