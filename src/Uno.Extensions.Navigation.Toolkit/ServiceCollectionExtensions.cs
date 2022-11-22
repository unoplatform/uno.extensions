using Uno.Extensions.Hosting;

namespace Uno.Extensions;

/// <summary>
/// Extension methods for adding services to an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
	private struct ToolkitViewInitializer : IRootViewInitializer
	{
		public ContentControl CreateDefaultView() => new LoadingView();

		public void InitializeViewHost(FrameworkElement element, Task loadingTask)
		{
			if (element is LoadingView loadingView)
			{
				loadingView.Source = new LoadingTask(loadingTask, element);
			}
		}

		public void PreInitialize(FrameworkElement element, IApplicationBuilder builder)
		{
			if (element is ExtendedSplashScreen splash)
			{
				splash.Initialize(builder.Window, builder.Arguments);
			}
		}
	}

	public static IApplicationBuilder AddToolkitNavigation(this IApplicationBuilder builder)
	{
		builder.Properties.Add(typeof(IRootViewInitializer), new ToolkitViewInitializer());
		return builder.Configure(host => host.ConfigureServices(
			 services => services.AddToolkitNavigation()));
	}

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

	/// <summary>
	/// Initializes navigation for an application using the LoadingView (from Uno Toolkit) to implement an extended splash screen
	/// Requires a Style for LoadingView with LoadingContent specified
	/// </summary>
	/// <param name="window">The application Window to initialize navigation for</param>
	/// <param name="buildHost">Function to create IHost</param>
	/// <param name="navigationRoot">LoadingView to host app navigation (only required for nesting navigation in an existing application)</param>
	/// <param name="initialRoute">[optional] Initial navigation route</param>
	/// <param name="initialView">[optional] Initial navigation view</param>
	/// <param name="initialViewModel">[optional] Initial navigation viewmodel</param>
	/// <param name="initialNavigate">[optional] Callback to drive initial navigation for app</param>
	/// <returns>The created IHost</returns>
	public static Task<IHost> InitializeNavigationAsync(
		this Window window,
		Func<Task<IHost>> buildHost,
		LoadingView navigationRoot,
		string? initialRoute = "",
		Type? initialView = null,
		Type? initialViewModel = null,
		Func<IServiceProvider, INavigator, Task>? initialNavigate = null)
	{
		return window.InternalInitializeNavigationAsync(
			buildHost,
			navigationRoot,
			initialRoute, initialView, initialViewModel,
			(root, navInit) =>
				{
					var loading = new LoadingTask(navInit, root);
					if (root is LoadingView lv)
					{
						lv.Source = loading;
					}
				},
			initialNavigate
			);
	}
}
