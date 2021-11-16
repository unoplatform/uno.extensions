using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Toolkit.Controls;
using Uno.Extensions.Navigation.Toolkit.Navigators;
using Uno.Toolkit.UI.Controls;

namespace Uno.Extensions.Navigation.Toolkit;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddToolkitNavigation(
		this IServiceCollection services)
	{
		return services
					.AddRegion<TabBar, TabBarNavigator>()

					.AddSingleton<INavigationBindingHandler, TabBarItemNavigationBindingHandler>();
	}

}
