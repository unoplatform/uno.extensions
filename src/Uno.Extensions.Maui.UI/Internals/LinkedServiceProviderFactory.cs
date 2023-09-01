namespace Uno.Extensions.Maui.Internals;

internal class LinkedServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
	private readonly IServiceProvider _childServiceProvider;

	public LinkedServiceProviderFactory(IServiceProvider childServiceProvider)
	{
		_childServiceProvider = childServiceProvider;
	}

	public IServiceCollection CreateBuilder(IServiceCollection services)
	{
		return services
			.AddSingleton<IServiceScopeFactory>(sp => new LinkedServiceScopeFactory(sp, _childServiceProvider));
	}

	public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
	{
		return new LinkedServiceProvider(containerBuilder.BuildServiceProvider(), _childServiceProvider);
	}
}
