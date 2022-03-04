using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.UI.Controls;
using Windows.UI.Popups;

namespace Uno.Extensions.Navigation;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddNavigation(
		this IServiceCollection services,
		Action<IViewRegistry, IRouteRegistry>? routeBuilder = null)
	{
		var views = new ViewRegistry();
		var routes = new RouteRegistry(services, views);
		routeBuilder?.Invoke(views, routes);

		return services
					.AddScoped<IInstanceRepository, InstanceRepository>()
					.AddSingleton<IResponseNavigatorFactory, ResponseNavigatorFactory>()

					.AddSingleton<RouteNotifier>()
					.AddSingleton<IRouteNotifier>(sp => sp.GetRequiredService<RouteNotifier>())
					.AddSingleton<IRouteUpdater>(sp => sp.GetRequiredService<RouteNotifier>())
					.AddScoped<Navigator>()

					.AddTransient<Flyout, NavigationFlyout>()

					// Register the region for each control type
					.AddRegion<Frame, FrameNavigator>()
					.AddRegion<ContentControl, ContentControlNavigator>()
				   .AddRegion<Panel, PanelVisiblityNavigator>(name: PanelVisiblityNavigator.NavigatorName)
				   .AddRegion<Microsoft.UI.Xaml.Controls.NavigationView, NavigationViewNavigator>()
					.AddRegion<ContentDialog, ContentDialogNavigator>(true)
					.AddRegion<MessageDialog, MessageDialogNavigator>(true)
					.AddRegion<Flyout, FlyoutNavigator>(true)
					.AddRegion<Popup, PopupNavigator>(true)

					.AddSingleton<IRequestHandler, ButtonBaseRequestHandler>()
					.AddSingleton<IRequestHandler, SelectorRequestHandler>()
					.AddSingleton<IRequestHandler, NavigationViewItemRequestHandler>()

					// Register the navigation mappings repository

					.AddSingleton<IViewRegistry>(views)
					.AddSingleton<IViewResolver, ViewResolver>()

					.AddSingleton<IRouteRegistry>(routes)
					.AddSingleton<RouteResolverDefault>()
					.AddSingleton<IRouteResolver>(sp => sp.GetRequiredService<RouteResolverDefault>())

					.AddSingleton<IResolver, Resolver>()


					.AddScoped<INavigatorFactory, NavigatorFactory>()

					.AddScopedInstance<IRegion>()

					.AddScopedInstance<NavigationRequest>()

					.AddScoped<NavigationDataProvider>()
					.AddScoped<RegionControlProvider>()
					.AddTransient<IDictionary<string, object>>(services => services.GetRequiredService<NavigationDataProvider>().Parameters)

					.AddScopedInstance<INavigator>();
	}

	public static IServiceCollection ConfigureNavigatorFactory(this IServiceCollection services, Action<INavigatorFactory> register)
	{
		return services.AddSingleton(new NavigatorFactoryBuilder() { Configure = register });
	}

	public static IServiceCollection AddScopedInstance<T>(this IServiceCollection services)
		where T : class
	{
#pragma warning disable CS8603 // Possible null reference return.
		return services.AddTransient<T>(sp => sp.GetInstance<T>());
#pragma warning restore CS8603 // Possible null reference return.
	}

	public static IServiceCollection AddRegion<TControl, TRegion>(this IServiceCollection services, bool isRequestRegion = false, string? name = null)
		where TRegion : class, INavigator
	{
		var names = name is not null ? new[] { name, typeof(TControl).Name } : new[] { typeof(TControl).Name };
		return services
				   .AddScoped<TRegion>()
				   .ConfigureNavigatorFactory(factory => factory.RegisterNavigator<TRegion>(isRequestRegion, names));
	}

}
