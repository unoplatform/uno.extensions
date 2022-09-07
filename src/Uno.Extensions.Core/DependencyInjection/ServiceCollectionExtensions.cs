using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Uno.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddNamedSingleton<TService, TImplementation>(this IServiceCollection services, string Name)
		where TService : class where TImplementation : class, TService
	{
		// Register the concrete type (the NamedTypeResolver will use this
		// rather than iterating through all the registered implementations
		// of TService
		services.TryAddSingleton<TImplementation>();
		// Register for TService
		services.TryAddSingleton<TService>(sp => sp.GetService<TImplementation>());
		return services
				// Register the named resolve
				.AddSingleton<INamedResolver<TService>>(new NamedTypeResolver<TService,TImplementation>(Name));
	}

	public static TService? GetNamedService<TService>(this IServiceProvider sp, string Name)
	{
		var resolver = sp.GetServices<INamedResolver<TService>>().FirstOrDefault(x=>x.Name==Name);
		return resolver.Resolve(sp);
	}

	public static TService GetNamedRequiredService<TService>(this IServiceProvider sp, string Name)
	{
		var resolver = sp.GetServices<INamedResolver<TService>>().FirstOrDefault(x => x.Name == Name);
		return resolver.ResolveRequired(sp);
	}
}
