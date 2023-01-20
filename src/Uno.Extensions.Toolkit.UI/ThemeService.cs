using Windows.Storage;
using Windows.UI.Xaml;

namespace Uno.Extensions.Toolkit;

internal class ThemeService : IThemeService
{
	private const string CurrentThemeSettingsKey = "CurrentTheme";
	private readonly Func<XamlRoot?> _xamlRootAccessor;
	private readonly IDispatcher _dispatcher;
	private readonly ILogger? _logger;

	/// <inheritdoc/>
	public event EventHandler<AppTheme>? DesiredThemeChanged;

	internal ThemeService(
		Func<XamlRoot?> xamlRootAccessor,
		IDispatcher dispatcher,
		ILogger? logger = default)
	{
		_xamlRootAccessor = xamlRootAccessor;
		_dispatcher = dispatcher;
		_logger = logger;
	}

	/// <inheritdoc/>
	public bool IsDark => _xamlRootAccessor() is { } xamlRoot ? SystemThemeHelper.IsRootInDarkMode(xamlRoot) : false;

	/// <inheritdoc/>
	public AppTheme Theme => GetSavedTheme();

	/// <inheritdoc/>
	public async Task SetThemeAsync(AppTheme theme)
	{
		await _dispatcher.ExecuteAsync(async () =>
		{
			if (_xamlRootAccessor() is { } xamlRoot)
			{
				if (theme != AppTheme.System)
				{
					SystemThemeHelper.SetRootTheme(xamlRoot, theme == AppTheme.Dark);
				}
				else
				{
					//Set System theme
					var systemTheme = SystemThemeHelper.GetCurrentOsTheme();
					SystemThemeHelper.SetRootTheme(xamlRoot, systemTheme == ApplicationTheme.Dark);
				}
			}
		});

		await SaveDesiredTheme(theme);
		DesiredThemeChanged?.Invoke(this, theme);
	}

	private async Task SaveDesiredTheme(AppTheme theme)
	{
		try
		{
			ApplicationData.Current.LocalSettings.Values[CurrentThemeSettingsKey] = theme.ToString();
		}
		catch (Exception ex)
		{
			if (_logger?.IsEnabled(LogLevel.Error) ?? false) _logger.LogError(ex, $"[ThemeService.SaveDesiredTheme({theme})] - Error while updating current theme.");
		}
	}

	private AppTheme GetSavedTheme()
	{
		try
		{
			return Enum.TryParse<AppTheme>(ApplicationData.Current.LocalSettings.Values[CurrentThemeSettingsKey] + string.Empty, out var theme) ? theme : AppTheme.System;
		}
		catch (Exception ex)
		{
			if (_logger?.IsEnabled(LogLevel.Error) ?? false) _logger.LogErrorMessage(ex, $"[ThemeService.GetSavedTheme()] - Error while reading stored theme.");
		}

		return AppTheme.System;
	}
}
