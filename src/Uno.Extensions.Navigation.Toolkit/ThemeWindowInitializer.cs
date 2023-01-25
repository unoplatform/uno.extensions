namespace Uno.Extensions;

internal class ThemeWindowInitializer : IWindowInitializer
{
	public async ValueTask InitializeWindowAsync(Window window)
	{
		var root = window.Content as FrameworkElement;
		if (root is null)
		{
			return;
		}
		var sp = root.FindServiceProvider();
		// Resolving the IThemeService should be enough to trigger the initialization
		// See internal InitializeAsync method on ThemeService
		var themeService = sp?.GetService<IThemeService>();
		if (themeService is null)
		{
			return;
		}
	}
}
