namespace Uno.Extensions.Toolkit;

internal class ScopedThemeService : ThemeService
{
	public ScopedThemeService(
		ILogger<ScopedThemeService> logger,
		Window window,
		IDispatcher dispatcher,
		ISettings settings) : base(window, dispatcher, settings, logger)
	{
	}
}
