namespace Uno.Extensions;

/// <summary>
/// Extension methods for <see cref="UIElement"/>
/// </summary>
public static class UIElementExtensions
{
	/// <summary>
	/// Returns a theme service for the given element
	/// </summary>
	/// <param name="element">The UIElement to use to build the theme service</param>
	/// <param name="logger">[Optional] logger for logging</param>
	/// <returns>Theme service for controlling application theme</returns>
	public static IThemeService GetThemeService(this UIElement element, ILogger? logger = default) =>
		new ThemeService(element, new Dispatcher(element), new Settings(), logger);
}
