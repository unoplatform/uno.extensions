using Microsoft.UI.Xaml;

namespace Uno.Extensions.Hosting;

/// <summary>
/// Extensions for the <see cref="IApplicationBuilder" />
/// </summary>
public static class IApplicationExtensions
{
	/// <summary>
	/// Creates an instance of the <see cref="IApplicationBuilder" /> for the given <see cref="Application" />
	/// </summary>
	/// <param name="app">The <see cref="Application" /></param>
	/// <param name="args">The <see cref="LaunchActivatedEventArgs" /> passed to OnLaunched.</param>
	/// <returns></returns>
	public static IApplicationBuilder CreateBuilder(this Application app, LaunchActivatedEventArgs args) =>
		new ApplicationBuilder(app, args);
}
