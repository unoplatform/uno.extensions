namespace Uno.Extensions.Hosting;

/// <summary>
/// Extensions for the <see cref="IApplicationBuilder" />
/// </summary>
public static class ApplicationExtensions
{
	/// <summary>
	/// Creates an instance of the <see cref="IApplicationBuilder" /> for the given <see cref="Application" />
	/// </summary>
	/// <param name="app">The <see cref="Application" /></param>
	/// <param name="args">The <see cref="LaunchActivatedEventArgs" /> passed to OnLaunched.</param>
	/// <returns></returns>
	public static IApplicationBuilder CreateBuilder(this Application app, LaunchActivatedEventArgs args) =>
		new ApplicationBuilder(app, args, app.GetType().Assembly);

	/// <summary>
	/// Creates an instance of the <see cref="IApplicationBuilder" /> for the given <see cref="Application" />
	/// </summary>
	/// <param name="app">The <see cref="Application" /></param>
	/// <param name="args">The <see cref="LaunchActivatedEventArgs" /> passed to OnLaunched.</param>
	/// <param name="applicationAssembly">The application assembly.</param>
	/// <returns></returns>
	public static IApplicationBuilder CreateBuilder(this Application app, LaunchActivatedEventArgs args, Assembly applicationAssembly) =>
		new ApplicationBuilder(app, args, applicationAssembly);

	/// <summary>
	/// Creates an instance of the <see cref="IApplicationBuilder" /> for the given <see cref="Application" /> with a custom Window factory
	/// </summary>
	/// <param name="app">The <see cref="Application" /></param>
	/// <param name="args">The <see cref="LaunchActivatedEventArgs" /> passed to OnLaunched.</param>
	/// <param name="windowFactory">A factory function to create a custom Window instance.</param>
	/// <returns></returns>
	public static IApplicationBuilder CreateBuilder(this Application app, LaunchActivatedEventArgs args, Func<Window> windowFactory) =>
		new ApplicationBuilder(app, args, app.GetType().Assembly, windowFactory);

	/// <summary>
	/// Creates an instance of the <see cref="IApplicationBuilder" /> for the given <see cref="Application" /> with a custom Window factory
	/// </summary>
	/// <param name="app">The <see cref="Application" /></param>
	/// <param name="args">The <see cref="LaunchActivatedEventArgs" /> passed to OnLaunched.</param>
	/// <param name="applicationAssembly">The application assembly.</param>
	/// <param name="windowFactory">A factory function to create a custom Window instance.</param>
	/// <returns></returns>
	public static IApplicationBuilder CreateBuilder(this Application app, LaunchActivatedEventArgs args, Assembly applicationAssembly, Func<Window> windowFactory) =>
		new ApplicationBuilder(app, args, applicationAssembly, windowFactory);
}
