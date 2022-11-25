namespace Uno.Extensions;

/// <summary>
/// Extension methods for adding services to an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
	private static bool _didRegisterServices;
	/// <summary>
	/// Adds navigation support for toolkit controls such as TabBar and DrawControl
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	public static IServiceCollection AddToolkitNavigation(
		this IServiceCollection services)
	{
		if (_didRegisterServices)
		{
			return services;
		}

		_didRegisterServices = true;
		return services
					.AddTransient<Flyout, ModalFlyout>()

					.AddRegion<TabBar, TabBarNavigator>()

					.AddRegion<DrawerControl, DrawerControlNavigator>()

					.AddSingleton<IRequestHandler, TabBarItemRequestHandler>();
	}
}
