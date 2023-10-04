#if MAUI_EMBEDDING
namespace Uno.Extensions.Maui;
internal class MauiHostBuilderServiceProviderFactory : IServiceProviderFactory<IServiceProvider>
{
	private readonly IServiceProvider _serviceProvider;

	public MauiHostBuilderServiceProviderFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

	public IServiceProvider CreateBuilder(IServiceCollection services) => _serviceProvider;
	public IServiceProvider CreateServiceProvider(IServiceProvider containerBuilder) => _serviceProvider;
}
#endif
