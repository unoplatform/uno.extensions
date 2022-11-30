namespace Uno.Extensions.Toolkit.UI;

public class ThemeService : IThemeService
{
	private readonly Window _window;
	private readonly IDispatcher _dispatcher;
	private readonly ILogger<ThemeService> _logger;
	private readonly IWritableOptions<ThemeSettings> _writeSettings;

	/// <inheritdoc/>
	public event EventHandler<DesiredTheme>? DesiredThemeChanged;

	public ThemeService(Window window, IDispatcher dispatcher,ILogger<ThemeService> logger, IWritableOptions<ThemeSettings> writeSettings)
	{
		_window = window;
		_dispatcher = dispatcher;
		_writeSettings = writeSettings;
		_logger = logger;
	}

	/// <inheritdoc/>
	public bool IsDark => SystemThemeHelper.IsRootInDarkMode(_window.Content.XamlRoot!);

	/// <inheritdoc/>
	public DesiredTheme Theme => GetSavedTheme();

	/// <inheritdoc/>
	public async Task SetThemeAsync(DesiredTheme theme)
	{
		if (theme != DesiredTheme.System)
		{
			await _dispatcher.ExecuteAsync(async () =>
			{
				SystemThemeHelper.SetRootTheme(_window.Content.XamlRoot, theme == DesiredTheme.Dark);
			});

		}
		else
		{
			//Set System theme
			var systemTheme = SystemThemeHelper.GetCurrentOsTheme();
			SystemThemeHelper.SetRootTheme(_window.Content.XamlRoot, systemTheme == ApplicationTheme.Dark);
		}

		await SaveDesiredTheme(theme);
		DesiredThemeChanged?.Invoke(this, theme);
	}

	private async Task SaveDesiredTheme(DesiredTheme theme)
	{
		try
		{
			await _writeSettings.UpdateAsync(themeSetting => themeSetting with { CurrentTheme = theme });
		}
		catch(Exception ex)
		{
			_logger?.LogError(ex, $"[ThemeService.SaveDesiredTheme({theme})] - Error while updating current theme.");
		}
	}

	private DesiredTheme GetSavedTheme()
	{
		try
		{
			return _writeSettings.Value.CurrentTheme;
		}
		catch (Exception ex)
		{
			_logger?.LogError(ex, $"[ThemeService.GetSavedTheme()] - Error while reading stored theme.");
		}

		return DesiredTheme.System;
	}
}
