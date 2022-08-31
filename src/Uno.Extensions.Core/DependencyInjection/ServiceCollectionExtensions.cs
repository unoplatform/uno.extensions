namespace Uno.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddNamedSingleton<TService, TImplementation>(this IServiceCollection services, string Name)
		where TService : class where TImplementation : class, TService
	{
		return services
				.AddSingleton<TService, TImplementation>()
				.AddSingleton<INamedResolver<TService>>(new NamedTypeResolver<TService,TImplementation>(Name));
	}

	public static TService? GetNamedService<TService>(this IServiceProvider sp, string Name)
	{
		var services = sp.GetServices<TService>();
		var resolver = sp.GetServices<INamedResolver<TService>>().FirstOrDefault(x=>x.Name==Name);
		return resolver.Resolve(services);
	}
}
