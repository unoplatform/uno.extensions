using Windows.Storage;

namespace Uno.Extensions.Toolkit;

internal class ThemeService : IThemeService
{
	private const string CurrentThemeSettingsKey = "CurrentTheme";
	private UIElement? _rootAccessorElement;
	private readonly IDispatcher _dispatcher;
	private readonly ILogger? _logger;
	private readonly TaskCompletionSource<bool> _initialization = new TaskCompletionSource<bool>();

	/// <inheritdoc/>
	public event EventHandler<AppTheme>? ThemeChanged;

	internal ThemeService(
		Window window,
		IDispatcher dispatcher,
		ILogger? logger = default)
	{
		_dispatcher = dispatcher;
		_logger = logger;

		_ = _dispatcher.ExecuteAsync(async ct =>
		{
			_rootAccessorElement = window.Content;
			await InitializeAsync();
		});
	}

	internal ThemeService(
		UIElement rootAccessorElement,
		IDispatcher dispatcher,
		ILogger? logger = default)
	{
		_rootAccessorElement = rootAccessorElement;
		_dispatcher = dispatcher;
		_logger = logger;

		_ = InitializeAsync();
	}

	/// <inheritdoc/>
	public bool IsDark => _rootAccessorElement?.XamlRoot is { } xamlRoot ? SystemThemeHelper.IsRootInDarkMode(xamlRoot) : false;

	/// <inheritdoc/>
	public AppTheme Theme => GetSavedTheme();


	/// <inheritdoc/>
	public async Task<bool> SetThemeAsync(AppTheme theme)
	{
		// Make sure initialization completes before attempting to set new theme
		await _initialization.Task;

		return await InternalSetThemeAsync(theme);
	}

	private async Task<bool> InternalSetThemeAsync(AppTheme theme)
	{
		return await _dispatcher.ExecuteAsync(async (ct) =>
		{
			var existingIsDark = IsDark;
			if (_rootAccessorElement?.XamlRoot is { } xamlRoot)
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

				await SaveDesiredTheme(theme);

				if (existingIsDark != IsDark)
				{
					ThemeChanged?.Invoke(this, theme);
				}
				return true;
			}
			return false;
		});

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

	private async Task InitializeAsync()
	{
		var theme = GetSavedTheme();
		var success = await InternalSetThemeAsync(theme);
		if (!success)
		{
			if (_rootAccessorElement is FrameworkElement fe)
			{
				async void OnLoaded(object sender, RoutedEventArgs args)
				{
					fe.Loaded -= OnLoaded;
					await InternalSetThemeAsync(theme);
					_initialization.TrySetResult(true);
				}

				fe.Loaded += OnLoaded;
			}
			else
			{
				if (_logger?.IsEnabled(LogLevel.Warning) ?? false) _logger.LogWarningMessage("Unable to attach to Loaded event. Use FrameworkElement instead of UIElement");
				_initialization.TrySetResult(false);
			}
		}
		_initialization.TrySetResult(true);
	}
}
