namespace Uno.Extensions.Configuration;

internal record ConfigBuilder(IHostBuilder HostBuilder) : IConfigBuilder
{
	public IDictionary<object, object> Properties => HostBuilder.Properties;

	public IHost Build() => HostBuilder.Build();
	public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate) => HostBuilder.ConfigureAppConfiguration(configureDelegate);
	public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) => HostBuilder.ConfigureContainer(configureDelegate);
	public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate) => HostBuilder.ConfigureHostConfiguration(configureDelegate);
	public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate) => HostBuilder.ConfigureServices(configureDelegate);
	public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull => HostBuilder.UseServiceProviderFactory(factory);
	public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull => HostBuilder.UseServiceProviderFactory(factory);

	public bool UseEmbeddedSourceFiles { get; set; }
}
