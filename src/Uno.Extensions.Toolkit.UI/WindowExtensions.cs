namespace Uno.Extensions;

public static class WindowExtensions
{
	public static IThemeService GetThemeService(this Window window, ILogger? logger = default) =>
		new ThemeService(window, new Dispatcher(window), logger);
}
