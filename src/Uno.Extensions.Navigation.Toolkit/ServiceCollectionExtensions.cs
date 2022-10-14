using Windows.ApplicationModel.Activation;

namespace Uno.Extensions;

#if WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

/// <summary>
/// Extension methods for adding services to an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds navigation support for toolkit controls such as TabBar and DrawControl
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	public static IServiceCollection AddToolkitNavigation(
		this IServiceCollection services)
	{
		return services
					.AddTransient<Flyout, ModalFlyout>()

					.AddRegion<TabBar, TabBarNavigator>()

					.AddRegion<DrawerControl, DrawerControlNavigator>()

					.AddSingleton<IRequestHandler, TabBarItemRequestHandler>();
	}

	/// <summary>
	/// Initializes navigation for an application using the LoadingView (from Uno Toolkit) to implement an extended splash screen
	/// Requires a Style for LoadingView with LoadingContent specified
	/// </summary>
	/// <param name="window">The application Window to initialize navigation for</param>
	/// <param name="buildHost">Function to create IHost</param>
	/// <param name="launchArgs">The application launch args, which includes SplashScreen</param>
	/// <param name="initialRoute">[optional] Initial navigation route</param>
	/// <param name="initialView">[optional] Initial navigation view</param>
	/// <param name="initialViewModel">[optional] Initial navigation viewmodel</param>
	/// <param name="navigationRoot">[optional] Where to host app navigation (only required for nesting navigation in an existing application)</param>
	/// <returns>The created IHost</returns>
	public static Task<IHost> InitializeNavigationWithExtendedSplash(
		this Window window,
		Func<Task<IHost>> buildHost,
		LaunchActivatedEventArgs? launchArgs = null,
		string? initialRoute = "",
		Type? initialView = null,
		Type? initialViewModel = null,
		ContentControl? navigationRoot = null)
	{
#if !WINUI
		var splashscreen=launchArgs?.SplashScreen;
#elif WINDOWS
		var splashscreen = launchArgs?.UWPLaunchActivatedEventArgs.SplashScreen;
#else
		var splashscreen = default(SplashScreen?);
#endif
		return window.InitializeNavigation<ToolkitViewHostProvider>(
			buildHost,
			viewHost =>
			{
				if (viewHost is ExtendedSplashScreen esplash)
				{
					esplash.SplashScreen = splashscreen;
					esplash.Window = window;
				}
			},
			initialRoute, initialView, initialViewModel, navigationRoot);
	}
}
