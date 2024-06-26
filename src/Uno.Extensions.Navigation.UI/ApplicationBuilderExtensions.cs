namespace Uno.Extensions;

public static class ApplicationBuilderExtensions
{
	/// <summary>
	/// Creates the Application Shell and will initialize the Shell Content before creating
	/// the <see cref="IHost" /> and initializing the app with the initial navigation.
	/// </summary>
	/// <typeparam name="TShell">The <see cref="UIElement" /> to use for the App Shell.</typeparam>
	/// <param name="appBuilder">The <see cref="IApplicationBuilder" />.</param>
	/// <param name="initialNavigate">An optional Navigation Delegate to conditionally control where the app should navigate on launch.</param>
	/// <param name="doNotActivate">An optional flag to prevent the Window from activating right after initialization but only when host is completely loaded.</param>
	/// <returns>The <see cref="IHost" />.</returns>
	public static async Task<IHost> NavigateAsync<TShell>(this IApplicationBuilder appBuilder,
		Func<IServiceProvider, INavigator, Task>? initialNavigate = null, bool doNotActivate = false)
		where TShell : UIElement, new()
	{
		var appRoot = new TShell();
		var navRoot = appRoot as ContentControl;
		if (appRoot is IContentControlProvider contentProvider)
		{
			navRoot = contentProvider.ContentControl;
		}

		Action<Window, FrameworkElement, Task, bool> initializeViewHost = (_, _, _, _) => { };
		if (appBuilder.Properties.TryGetValue(typeof(IRootViewInitializer), out var propValue) && propValue is IRootViewInitializer initializer)
		{
			navRoot ??= initializer.CreateDefaultView();
			initializer.PreInitialize(navRoot, appBuilder);
			initializeViewHost = initializer.InitializeViewHost;
		}

		navRoot ??= new ContentControl
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch
		};

		appBuilder.Window.Content = appRoot;

		return await appBuilder.Window.InternalInitializeNavigationAsync(
			buildHost: () => Task.FromResult(appBuilder.Build()),
			navigationRoot: navRoot,
			initializeViewHost: initializeViewHost,
			initialNavigate: initialNavigate,
			doNotActivate: doNotActivate
		);
	}
}
