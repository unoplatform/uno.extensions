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
	/// Configures the application to use a custom Window instance
	/// </summary>
	/// <param name="builder">The <see cref="IApplicationBuilder" /></param>
	/// <param name="windowFactory">A factory function to create a custom Window instance.</param>
	/// <returns>The <see cref="IApplicationBuilder" /></returns>
	public static IApplicationBuilder UseWindow(this IApplicationBuilder builder, Func<Window> windowFactory)
	{
		if (builder is ApplicationBuilder appBuilder)
		{
			appBuilder.Window = windowFactory();
		}
		return builder;
	}
}
