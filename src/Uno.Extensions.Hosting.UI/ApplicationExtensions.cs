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
	public static IApplicationBuilder CreateBuilder(this Application app, LaunchActivatedEventArgs args, Assembly applicationAssembly) =>
		new ApplicationBuilder(app, args, applicationAssembly);
}
