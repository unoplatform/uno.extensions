#if MAUI_EMBEDDING
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Uno.Extensions.Maui;
internal class UnoServiceProviderFactory : IServiceProviderFactory<IServiceProvider>
{
	private readonly MauiAppBuilder _mauiAppBuilder;
	private readonly Action _buildAppBuilderCallback;

	public UnoServiceProviderFactory(MauiAppBuilder builder, Action buildAppBuilderCallback)
	{
		_mauiAppBuilder = builder;
		_buildAppBuilderCallback = buildAppBuilderCallback;
	}

	// We delay calling build on the MauiHostBuilder until the Uno Host is built
	public IServiceProvider CreateBuilder(IServiceCollection services)
	{
		// NOTE: TryAdd prevents duplicates from overriding what was registered with Uno's Host Builder such as IConfiguration
		var instanceDescriptors = new List<ServiceDescriptor>();
		_mauiAppBuilder.Services.ForEach(x =>
		{
			if (!services.TryAddService(x) )
			{
				if (x.ImplementationInstance is not null)
				{
					instanceDescriptors.Add(x);
				}
			}
		});

		var serviceProvider = services.BuildServiceProvider();
		instanceDescriptors.ForEach(x =>
		{
			if (serviceProvider is not null &&
				x.ImplementationInstance is not null)
			{
				var implType = x.ImplementationType ?? x.ImplementationInstance.GetType();

				serviceProvider.AddSingletonInstance(implType, x.ImplementationInstance);
			}
		});

		_mauiAppBuilder.ConfigureContainer(new MauiServiceProviderFactory(serviceProvider));
		_buildAppBuilderCallback();
		return serviceProvider;
	}

	public IServiceProvider CreateServiceProvider(IServiceProvider services) => services;
}
#endif
