using System;
using System.Collections.Generic;
using System.Text;
using Chinook.BackButtonManager;
using Chinook.SectionsNavigation;
using Chinook.StackNavigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApplicationTemplate
{
	/// <summary>
	/// This class is used for navigation configuration.
	/// - Configures the navigator.
	/// </summary>
	public static class NavigationCoreConfiguration
	{
		/// <summary>
		/// Adds the core navigation services to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services">Service collection.</param>
		/// <returns><see cref="IServiceCollection"/>.</returns>
		public static IServiceCollection AddNavigationCore(this IServiceCollection services)
		{
//-:cnd:noEmit
#if NETFRAMEWORK
//+:cnd:noEmit
			services.AddSingleton<ISectionsNavigator>(s => new BlindSectionsNavigator("Home", "Posts", "Settings"));
//-:cnd:noEmit
#endif
//+:cnd:noEmit

			return services
				.AddSingleton<IStackNavigator>(s => new SectionsNavigatorToStackNavigatorAdapter(s.GetService<ISectionsNavigator>()))
				.AddSingleton<IBackButtonManager>(s =>
				{
					var manager = new BackButtonManager();

					var sectionsNavigator = s.GetRequiredService<ISectionsNavigator>();
					manager.AddHandler(new BackButtonHandler(
						name: "DefaultSectionsNavigatorHandler",
						canHandle: () => sectionsNavigator.CanNavigateBackOrCloseModal(),
						handle: async ct => await sectionsNavigator.NavigateBackOrCloseModal(ct)
					));

					return manager;
				});
		}
	}
}
