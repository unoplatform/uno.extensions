using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Animations;

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
		var singletons = new List<(Type ServiceType, object Implementation)>();

		// Copy all Uno registrations into the Maui ServiceCollection
		services.ForEach(x =>
		{
			if (!_mauiAppBuilder.Services.TryAddService(x))
			{
				// Pick up any cases where Uno is registering a transient for a service type where Maui is registering a singleton
				// These are most likely cases where Uno is using the mutable instance container, so need to register
				// the singleton instances once the service provider is created
				var reg = _mauiAppBuilder.Services.FirstOrDefault(s => s.ServiceType == x.ServiceType);
				if (reg is not null &&
					reg.Lifetime is ServiceLifetime.Singleton &&
					reg.ImplementationInstance is not null && 
					x.Lifetime is ServiceLifetime.Transient)
				{
					singletons.Add((reg.ServiceType, reg.ImplementationInstance));
				}
				_mauiAppBuilder.Services.Add(x);
			}
		});

		// Create the service provider that's shared by Maui and Uno
		var serviceProvider = _mauiAppBuilder.Services.BuildServiceProvider();

		// Register the singleton instances captured earlier with the mutable instance container
		// TODO: Revisit this and ensure that we shouldn't refactor this... it's a bit of a hack
		singletons.ForEach(sreg => serviceProvider.AddSingletonInstance(sreg.ServiceType, sreg.Implementation));

		_mauiAppBuilder.ConfigureContainer(new MauiServiceProviderFactory(serviceProvider));
		_buildAppBuilderCallback();
		return serviceProvider;
	}

	public IServiceProvider CreateServiceProvider(IServiceProvider services) => services;
}
