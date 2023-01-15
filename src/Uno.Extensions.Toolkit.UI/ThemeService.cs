namespace Uno.Extensions.Toolkit;

internal class ThemeService : IThemeService
{
	private readonly Window _window;
	private readonly IDispatcher _dispatcher;
	private readonly ILogger<ThemeService> _logger;
	private readonly IWritableOptions<ThemeSettings> _writeSettings;

	/// <inheritdoc/>
	public event EventHandler<AppTheme>? DesiredThemeChanged;

	public ThemeService(
		ILogger<ThemeService> logger,
		Window window,
		IDispatcher dispatcher,
		IWritableOptions<ThemeSettings> writeSettings)
	{
		_window = window;
		_dispatcher = dispatcher;
		_writeSettings = writeSettings;
		_logger = logger;
	}

	/// <inheritdoc/>
	public bool IsDark => SystemThemeHelper.IsRootInDarkMode(_window.Content.XamlRoot!);

	/// <inheritdoc/>
	public AppTheme Theme => GetSavedTheme();

	/// <inheritdoc/>
	public async Task SetThemeAsync(AppTheme theme)
	{
		if (theme != AppTheme.System)
		{
			await _dispatcher.ExecuteAsync(async () =>
			{
				SystemThemeHelper.SetRootTheme(_window.Content.XamlRoot, theme == AppTheme.Dark);
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

	private async Task SaveDesiredTheme(AppTheme theme)
	{
		try
		{
			await _writeSettings.UpdateAsync(themeSetting => themeSetting with { CurrentTheme = theme });
		}
		catch(Exception ex)
		{
			if(_logger.IsEnabled(LogLevel.Error)) _logger.LogError(ex, $"[ThemeService.SaveDesiredTheme({theme})] - Error while updating current theme.");
		}
	}

	private AppTheme GetSavedTheme()
	{
		try
		{
			return _writeSettings.Value.CurrentTheme;
		}
		catch (Exception ex)
		{
			if (_logger.IsEnabled(LogLevel.Error)) _logger.LogErrorMessage(ex, $"[ThemeService.GetSavedTheme()] - Error while reading stored theme.");
		}

		return AppTheme.System;
	}
}
