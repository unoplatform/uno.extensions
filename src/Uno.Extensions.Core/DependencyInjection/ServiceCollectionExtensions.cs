using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Uno.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddNamedSingleton<TService, TImplementation>(this IServiceCollection services, string Name)
		where TService : class where TImplementation : class, TService
	{
		// Register the concrete type (the NamedInstance will use this
		// rather than iterating through all the registered implementations
		// of TService
		services.TryAddSingleton<TImplementation>();
		// Register for TService
		services.TryAddSingleton<TService>(sp => sp.GetService<TImplementation>());
		return services
				// Register the named resolve
				.AddSingleton<INamedInstance<TService>>(sp=>new NamedInstance<TService,TImplementation>(sp,Name));
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
