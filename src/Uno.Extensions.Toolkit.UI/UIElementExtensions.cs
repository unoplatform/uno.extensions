namespace Uno.Extensions;

public static class UIElementExtensions
{
	public static IThemeService? GetThemeService(this UIElement element, ILogger? logger = default) => element.XamlRoot is null ? default :
		new ThemeService(element.XamlRoot, new Dispatcher(element), logger);
}
