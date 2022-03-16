namespace Uno.Extensions.Hosting.Internal;

internal interface IServiceFactoryAdapter
{
	object CreateBuilder(IServiceCollection services);

	IServiceProvider CreateServiceProvider(object containerBuilder);
}
