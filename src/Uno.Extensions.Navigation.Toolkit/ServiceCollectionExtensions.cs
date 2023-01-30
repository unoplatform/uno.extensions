namespace Uno.Extensions;

/// <summary>
/// Extension methods for adding services to an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds navigation support for toolkit controls such as TabBar and DrawControl
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <param name="context">The <see cref="HostBuilderContext"/> to use when adding services</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	public static IServiceCollection AddToolkitNavigation(
		this IServiceCollection services,
		HostBuilderContext context)
	{
		if(context.IsRegistered(nameof(AddToolkitNavigation)))
		{
			return services;
		}

		return services
					.AddTransient<Flyout, ModalFlyout>()

					.AddRegion<TabBar, TabBarNavigator>()

					.AddRegion<DrawerControl, DrawerControlNavigator>()

					.AddSingleton<IRequestHandler, TabBarItemRequestHandler>()

					.AddSingleton<IWindowInitializer, ThemeWindowInitializer>();
	}
}
