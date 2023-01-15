namespace Uno.Extensions.DependencyInjection;

internal record NamedInstance<TService, TImplementation>(IServiceProvider Services, string Name) : INamedInstance<TService>
	where TService : class where TImplementation : class, TService
{
	private TService? _service;
	public TService? Get()
	{
		return _service ??= Services.GetService<TImplementation>();
	}

	public TService GetRequired()
	{
		return _service ??= Services.GetRequiredService<TImplementation>();
	}
}

