namespace Uno.Extensions.DependencyInjection;

internal record NamedInstanceReference<TService>(IServiceProvider Services, string OriginalName, string Name) : INamedInstance<TService>
{
	public TService? Get()
	{
		return Services.GetNamedService<TService>(OriginalName);
	}

	public TService GetRequired()
	{
		return Services.GetRequiredNamedService<TService>(OriginalName);
	}
}

