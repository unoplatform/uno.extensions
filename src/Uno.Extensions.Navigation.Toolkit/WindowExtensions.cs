namespace Uno.Extensions;

/// <summary>
/// Extension methods on <see cref="Window" />.
/// </summary>
public static class WindowExtensions
{
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
	/// <param name="doNotActivate">[optional] Do not activate the window after initializing navigation</param>
	/// <returns>The created IHost</returns>
	public static Task<IHost> InitializeNavigationAsync(
		this Window window,
		Func<Task<IHost>> buildHost,
		LoadingView navigationRoot,
		string? initialRoute = "",
		Type? initialView = null,
		Type? initialViewModel = null,
		Func<IServiceProvider, INavigator, Task>? initialNavigate = null,
		bool doNotActivate = false)
	{
		return window.InternalInitializeNavigationAsync(
			buildHost,
			navigationRoot,
			initialRoute, initialView, initialViewModel,
			ApplyLoadingTask,
			initialNavigate,
			doNotActivate
			);
	}

	internal static void ApplyLoadingTask(this Window window, FrameworkElement root, Task navInit, bool doNotActivate)
	{
		var activate = true;
		if (root is LoadingView lv)
		{
			var loadingTask = navInit;
			if (lv is ExtendedSplashScreen splash)
			{
				if (!splash.SplashIsEnabled)
				{
					// Splash isn't enabled, so don't activate until loading completed
					activate = false;

					splash.UseTransitions = false;

					loadingTask = new Func<Task>(async () =>
					{
						await navInit;
						if (!doNotActivate)
							window.Activate();
					})();
				}
			}
			var loading = new LoadingTask(loadingTask, root);
			lv.Source = loading;
		}

		if (activate && !doNotActivate)
		{
			// Activate immediately to show the splash screen
			window.Activate();
		}
	}
}
