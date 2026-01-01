using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions;

/// <summary>
/// Extensions for configuring navigation.
/// </summary>
public static class HostBuilderExtensions
{
	internal const string RequiresDynamicCodeMessage = "Binding strongly typed objects to configuration values may require generating dynamic code at runtime. [From Array.CreateInstance() and others.]";
	internal const string RequiresUnreferencedCodeMessage = "Cannot statically analyze the type of instance so its members may be trimmed. [From TypeDescriptor.GetConverter() and others.]";

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
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IHostBuilder UseNavigation(
		this IHostBuilder hostBuilder,
		Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder,
		Func<IServiceCollection, IViewRegistry>? createViewRegistry,
		Func<IServiceCollection, IRouteRegistry>? createRouteRegistry,
		Func<NavigationConfiguration, NavigationConfiguration>? configure,
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
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IHostBuilder UseNavigation(
		this IHostBuilder hostBuilder,
		Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder = null,
		Func<IServiceCollection, IViewRegistry>? createViewRegistry = null,
		Func<IServiceCollection, IRouteRegistry>? createRouteRegistry = null,
		Func<NavigationConfiguration, NavigationConfiguration>? configure = null,
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
						.Section<NavigationConfiguration>(nameof(NavigationConfiguration)))
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
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IHostBuilder UseNavigation(
		this IHostBuilder hostBuilder,
		IDictionary<Type, Type> viewModelMapping,
		Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder = null,
		Func<IServiceCollection, MappedViewRegistry>? createMappedViewRegistry = null,
		Func<IServiceCollection, IRouteRegistry>? createRouteRegistry = null,
		Func<NavigationConfiguration, NavigationConfiguration>? configure = null,
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
