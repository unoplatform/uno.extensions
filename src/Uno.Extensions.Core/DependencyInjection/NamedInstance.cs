namespace Uno.Extensions.DependencyInjection;

internal record NamedInstance<TService, TImplementation>(IServiceProvider Services, string Name) : INamedInstance<TService>
	where TService : class where TImplementation : class, TService
{
	public TService? Get()
	{
		return Services.GetService<TImplementation>();
	}

	public TService GetRequired()
	{
		return Services.GetRequiredService<TImplementation>();
	}
}

