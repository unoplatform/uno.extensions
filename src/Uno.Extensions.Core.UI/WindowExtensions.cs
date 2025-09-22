using System.Reflection;

namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="Window"/>
/// </summary>
public static partial class WindowExtensions
{
	/// <summary>
	/// Gets the theme service for the window
	/// </summary>
	/// <param name="window">The window to use to build the theme service</param>
	/// <param name="logger">[Optional]The logger for log output</param>
	/// <returns>The theme service for controlling application theme</returns>
	public static IThemeService GetThemeService(this Window window, ILogger? logger = default) =>
		new ThemeService(window, new Dispatcher(window), new Settings(), logger);
}
