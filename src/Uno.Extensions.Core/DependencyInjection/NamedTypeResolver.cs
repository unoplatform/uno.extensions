namespace Uno.Extensions.DependencyInjection;

internal record NamedTypeResolver<TService, TImplementation>(string Name) : INamedResolver<TService>
	where TService : class where TImplementation : class, TService
{
	public TService? Resolve(IServiceProvider services)
	{
		return services.GetService<TImplementation>();
	}

	public TService ResolveRequired(IServiceProvider services)
	{
		return services.GetRequiredService<TImplementation>();
	}
}
