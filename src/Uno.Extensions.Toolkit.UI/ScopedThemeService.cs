namespace Uno.Extensions.Toolkit;

internal class ScopedThemeService : ThemeService
{
	public ScopedThemeService(
		ILogger<ScopedThemeService> logger,
		Window window,
		IDispatcher dispatcher) : base(window, dispatcher, logger)
	{
	}
}
