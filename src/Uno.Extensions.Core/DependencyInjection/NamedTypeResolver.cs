namespace Uno.Extensions.DependencyInjection;

internal record NamedTypeResolver<TService, TImplementation>(string Name) : INamedResolver<TService>
	where TService : class where TImplementation : class, TService
{
	public TService? Resolve(IEnumerable<TService> instances)
	{
		return instances.FirstOrDefault(x => x is TImplementation);
	}

}
