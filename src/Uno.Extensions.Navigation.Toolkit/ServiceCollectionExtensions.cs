using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions;
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.Toolkit.Controls;
using Uno.Extensions.Navigation.Toolkit.Navigators;
using Uno.Toolkit.UI;

namespace Uno.Extensions.Navigation.Toolkit;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddToolkitNavigation(
		this IServiceCollection services)
	{
		return services
					.AddTransient<Flyout, ModalFlyout>()

					.AddRegion<TabBar, TabBarNavigator>()

					.AddSingleton<IRequestHandler, TabBarItemRequestHandler>();
	}
}
