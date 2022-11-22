namespace Uno.Extensions.Navigation;

public static class ApplicationBuilderExtensions
{
	public static Task<IHost> ShowAsync<TShell>(this IApplicationBuilder appBuilder)
		where TShell : UIElement, new() =>
		NavigateInternalAsync<TShell>(appBuilder, null);

	public static Task<IHost> NavigateAsync<TShell>(this IApplicationBuilder appBuilder,
		Func<IServiceProvider, INavigator, Task> initialNavigate)
		where TShell : UIElement, new() =>
		NavigateInternalAsync<TShell>(appBuilder, initialNavigate);

	private static async Task<IHost> NavigateInternalAsync<TShell>(IApplicationBuilder appBuilder,
		Func<IServiceProvider, INavigator, Task>? initialNavigate = null)
		where TShell : UIElement, new()
	{
		var appRoot = new TShell();
		var navRoot = appRoot as ContentControl;
		if (appRoot is IContentControlProvider contentProvider)
		{
			navRoot = contentProvider.ContentControl;
		}

		Action<FrameworkElement, Task> initializeViewHost = (_, _) => { };
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
		appBuilder.Window.Activate();

		return await appBuilder.Window.InternalInitializeNavigationAsync(
			buildHost: () => Task.FromResult(appBuilder.Build()),
			navigationRoot: navRoot,
			initializeViewHost: initializeViewHost,
			initialNavigate: initialNavigate
		);
	}
}
