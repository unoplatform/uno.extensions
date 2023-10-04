#if MAUI_EMBEDDING
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Uno.Extensions.Maui;
internal class UnoHostBuilderServiceProviderFactory : IServiceProviderFactory<IServiceProvider>
{
	private readonly MauiAppBuilder _mauiAppBuilder;
	private readonly Action _buildAppBuilderCallback;

	public UnoHostBuilderServiceProviderFactory(MauiAppBuilder builder, Action buildAppBuilderCallback)
	{
		_mauiAppBuilder = builder;
		_buildAppBuilderCallback = buildAppBuilderCallback;
	}

	// We delay calling build on the MauiHostBuilder until the Uno Host is built
	public IServiceProvider CreateBuilder(IServiceCollection services)
	{
		// NOTE: TryAdd prevents duplicates from overriding what was registered with Uno's Host Builder such as IConfiguration
		_mauiAppBuilder.Services.ForEach(x => services.TryAdd(x));

		var serviceProvider = services.BuildServiceProvider();
		_mauiAppBuilder.ConfigureContainer(new MauiHostBuilderServiceProviderFactory(serviceProvider));
		_buildAppBuilderCallback();
		return serviceProvider;
	}

	public IServiceProvider CreateServiceProvider(IServiceProvider services) => services;
}
#endif
