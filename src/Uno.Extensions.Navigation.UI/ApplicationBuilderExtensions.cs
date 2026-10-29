using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions;

public static class ApplicationBuilderExtensions
{
	internal const string RequiresUnreferencedCodeMessage = "Cannot statically analyze the type of instance so its members may be trimmed. [From TypeDescriptor.GetConverter() and others.]";

	/// <summary>
	/// Creates the Application Shell and will initialize the Shell Content before creating
	/// the <see cref="IHost" /> and initializing the app with the initial navigation.
	/// </summary>
	/// <typeparam name="TShell">
	/// The <see cref="UIElement" /> to use for the App Shell. It must either derive from <see cref="ContentControl" />
	/// or implement <see cref="IContentControlProvider" /> to supply the <see cref="ContentControl" /> that hosts navigation.
	/// </typeparam>
	/// <param name="appBuilder">The <see cref="IApplicationBuilder" />.</param>
	/// <param name="initialNavigate">An optional Navigation Delegate to conditionally control where the app should navigate on launch.</param>
	/// <returns>The <see cref="IHost" />.</returns>
	/// <exception cref="InvalidOperationException">
	/// <typeparamref name="TShell" /> neither derives from <see cref="ContentControl" /> nor implements <see cref="IContentControlProvider" />
	/// (e.g. a <see cref="Page" /> or a plain <see cref="UserControl" />).
	/// </exception>
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static async Task<IHost> NavigateAsync<TShell>(this IApplicationBuilder appBuilder,
		Func<IServiceProvider, INavigator, Task>? initialNavigate = null)
		where TShell : UIElement, new()
	{
		var appRoot = new TShell();
		var navRoot = appRoot as ContentControl;
		if (appRoot is IContentControlProvider contentProvider)
		{
			navRoot = contentProvider.ContentControl;
		}

		// The shell becomes Window.Content, so a navigation root that isn't part of it would never
		// be loaded and the initial navigation would park forever (a silent blank screen).
		// Page and UserControl derive from Control (not ContentControl) since Uno 7.
		if (navRoot is null)
		{
			throw new InvalidOperationException(
				$"The shell type '{typeof(TShell).FullName}' must derive from {nameof(ContentControl)} or implement {nameof(IContentControlProvider)} to host navigation.");
		}

		Action<Window, FrameworkElement, Task> initializeViewHost = (_, _, _) => { };
		if (appBuilder.Properties.TryGetValue(typeof(IRootViewInitializer), out var propValue) && propValue is IRootViewInitializer initializer)
		{
			initializer.PreInitialize(navRoot, appBuilder);
			initializeViewHost = initializer.InitializeViewHost;
		}

		appBuilder.Window.Content = appRoot;

		return await appBuilder.Window.InternalInitializeNavigationAsync(
			buildHost: () => Task.FromResult(appBuilder.Build()),
			navigationRoot: navRoot,
			initializeViewHost: initializeViewHost,
			initialNavigate: initialNavigate
		);
	}
}
