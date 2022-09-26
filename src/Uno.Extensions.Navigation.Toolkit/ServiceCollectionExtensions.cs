namespace Uno.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddToolkitNavigation(
		this IServiceCollection services)
	{
		return services
					.AddTransient<Flyout, ModalFlyout>()

					.AddRegion<TabBar, TabBarNavigator>()

					.AddRegion<DrawerControl, DrawerControlNavigator>()

					.AddSingleton<IRequestHandler, TabBarItemRequestHandler>();
	}

	public static Task<IHost> InitializeNavigationWithExtendedSplash(this Window window, Func<IHost> buildHost, string? initialRoute = "", Type? initialView = null, Type? initialViewModel = null)
	{
		return window.InitializeNavigation<ToolkitViewHostProvider>(buildHost, initialRoute, initialView, initialViewModel);
	}
}
