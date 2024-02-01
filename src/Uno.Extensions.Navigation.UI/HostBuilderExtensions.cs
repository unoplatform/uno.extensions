namespace Uno.Extensions;

/// <summary>
/// Extensions for configuring navigation.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Configures navigation services
	/// </summary>
	/// <param name="hostBuilder">The host builder to configure</param>
	/// <param name="viewRouteBuilder">Callback to define view and route maps</param>
	/// <param name="createViewRegistry">Callback to create IViewRegistry implementation</param>
	/// <param name="createRouteRegistry">Callback to create IRouteRegistry implementation</param>
	/// <param name="configure">Callback to adjust navigation configuration (default should be to use appsettings.json)</param>
	/// <param name="configureServices">Callback to register other services related to navigation</param>
	/// <returns>The host builder</returns>
	public static IHostBuilder UseNavigation(
		this IHostBuilder hostBuilder,
		Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder,
		Func<IServiceCollection, IViewRegistry>? createViewRegistry,
		Func<IServiceCollection, IRouteRegistry>? createRouteRegistry,
		Func<NavigationConfig, NavigationConfig>? configure,
		Action<IServiceCollection> configureServices)
	{
		return hostBuilder.UseNavigation(
			viewRouteBuilder,
			createViewRegistry,
			createRouteRegistry,
			configure,
			(context, builder) => configureServices.Invoke(builder));
	}

	/// <summary>
	/// Configures navigation services
	/// </summary>
	/// <param name="hostBuilder">The host builder to configure</param>
	/// <param name="viewRouteBuilder">Callback to define view and route maps</param>
	/// <param name="createViewRegistry">Callback to create IViewRegistry implementation</param>
	/// <param name="createRouteRegistry">Callback to create IRouteRegistry implementation</param>
	/// <param name="configure">Callback to adjust navigation configuration (default should be to use appsettings.json)</param>
	/// <param name="configureServices">Callback to register other services related to navigation</param>
	/// <returns>The host builder</returns>
	public static IHostBuilder UseNavigation(
		this IHostBuilder hostBuilder,
		Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder = null,
		Func<IServiceCollection, IViewRegistry>? createViewRegistry = null,
		Func<IServiceCollection, IRouteRegistry>? createRouteRegistry = null,
		Func<NavigationConfig, NavigationConfig>? configure = null,
		Action<HostBuilderContext, IServiceCollection>? configureServices = default)
	{
		if (hostBuilder.IsRegistered(nameof(UseNavigation)))
		{
			return hostBuilder;
		}
		return hostBuilder
			.UseConfiguration(
				configure: configBuilder =>
					configBuilder
						.Section<NavigationConfig>(nameof(NavigationConfig)))
			.ConfigureServices((ctx, services) =>
			{
				_ = services.AddNavigation(configure, viewRouteBuilder, createViewRegistry, createRouteRegistry);
				configureServices?.Invoke(ctx, services);
			});
	}

	/// <summary>
	/// Configures navigation services
	/// </summary>
	/// <param name="hostBuilder">The host builder to configure</param>
	/// <param name="viewModelMapping">The dictionary to map remap viewmodel types (used by MVUX)</param>
	/// <param name="viewRouteBuilder">Callback to define view and route maps</param>
	/// <param name="createMappedViewRegistry">Callback to create MappedViewRegistry implementation</param>
	/// <param name="createRouteRegistry">Callback to create IRouteRegistry implementation</param>
	/// <param name="configure">Callback to adjust navigation configuration (default should be to use appsettings.json)</param>
	/// <param name="configureServices">Callback to register other services related to navigation</param>
	/// <returns>The host builder</returns>
	public static IHostBuilder UseNavigation(
		this IHostBuilder hostBuilder,
		IDictionary<Type, Type> viewModelMapping,
		Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder = null,
		Func<IServiceCollection, MappedViewRegistry>? createMappedViewRegistry = null,
		Func<IServiceCollection, IRouteRegistry>? createRouteRegistry = null,
		Func<NavigationConfig, NavigationConfig>? configure = null,
		Action<HostBuilderContext, IServiceCollection>? configureServices = default)
	{
		return hostBuilder.UseNavigation(
					viewRouteBuilder,
					createViewRegistry: sc => createMappedViewRegistry?.Invoke(sc) ?? new MappedViewRegistry(sc, viewModelMapping),
					createRouteRegistry: createRouteRegistry,
					configure: configure,
					configureServices: (ctx, services) =>
				{
					configureServices?.Invoke(ctx, services);
					services
						.AddSingleton<IRouteResolver, MappedRouteResolver>();
				});
	}
}
