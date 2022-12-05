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
		var themeService = sp?.GetService<IThemeService>();
		if (themeService is null)
		{
			return;
		}

		var desired = themeService.Theme;
		if (root.XamlRoot is not null)
		{
			await themeService.SetThemeAsync(desired);
		}
		else
		{
			root.Loaded += async (_, _) =>
			{
				await themeService.SetThemeAsync(desired);
			};
		}

	}
}
