namespace Uno.Extensions.Toolkit;

internal class ThemeService : IThemeService, IDisposable
{
	private const string CurrentThemeSettingsKey = "CurrentTheme";
	private UIElement? _rootAccessorElement;
	private readonly IDispatcher _dispatcher;
	private readonly ILogger? _logger;
	private readonly ISettings _settings;
	private TaskCompletionSource<bool>? _initialization;

	/// <inheritdoc/>
	public event EventHandler<AppTheme>? ThemeChanged;

	internal ThemeService(
		Window window,
		IDispatcher dispatcher,
		ISettings settings,
		ILogger? logger = default)
	{
		_dispatcher = dispatcher;
		_logger = logger;
		_settings = settings;

		if (!_dispatcher.HasThreadAccess && PlatformHelper.IsThreadingEnabled)
		{
			// Need to dispatch in order to access window.Content on UI thread
			_ = _dispatcher.ExecuteAsync(InitWindow);
		}
		else
		{
			_ = InitWindow(CancellationToken.None);
		}

		async ValueTask InitWindow(CancellationToken ct)
		{
			RootElement = window.Content;
			await InitializeAsync();
		}
	}

	internal ThemeService(
		UIElement rootAccessorElement,
		IDispatcher dispatcher,
		ISettings settings,
		ILogger? logger = default)
	{
		RootElement = rootAccessorElement;
		_dispatcher = dispatcher;
		_logger = logger;
		_settings = settings;

		_ = InitializeAsync();
	}

	/// <inheritdoc/>
	public bool IsDark => (RootElement as FrameworkElement)?.ActualTheme == ElementTheme.Dark;

	/// <inheritdoc/>
	public AppTheme Theme => GetSavedTheme();

	private UIElement? RootElement
	{
		get => _rootAccessorElement;
		set
		{
			if(_rootAccessorElement == value)
			{
				return;
			}
			if (_rootAccessorElement is FrameworkElement oldElement)
			{
				oldElement.ActualThemeChanged -= ElementThemeChanged;
			}
			_rootAccessorElement = value;
			if(_rootAccessorElement is FrameworkElement element)
			{
				element.ActualThemeChanged += ElementThemeChanged;
			}
		}
	}

	private void ElementThemeChanged(FrameworkElement sender, object args)
	{
		var savedThemePreference = GetSavedTheme();

		// Only respond to system-driven changes if the user's preference is explicitly 'System'.
		// If the user has set an explicit Dark or Light preference, ignore system theme changes.
		if (savedThemePreference == AppTheme.System)
		{
			ThemeChanged?.Invoke(this, savedThemePreference);
		}
	}

	/// <inheritdoc/>
	public async Task<bool> SetThemeAsync(AppTheme theme)
	{
		if (_initialization is null)
		{
			throw new NullReferenceException($"Theme service not initialized, {nameof(InitializeAsync)} needs to complete before SetThemeAsync can be called");
		}

		// Make sure initialization completes before attempting to set new theme
		await _initialization.Task;

		return await InternalSetThemeAsync(theme);
	}

	private async Task<bool> InternalSetThemeAsync(AppTheme theme)
	{
		if (_dispatcher.HasThreadAccess ||
			(!PlatformHelper.IsThreadingEnabled && !(_initialization?.Task.IsCompleted??false)))
		{
			return InternalSetThemeOnUIThread(theme);
		}
		else
		{
			return await _dispatcher.ExecuteAsync(async _ => InternalSetThemeOnUIThread(theme));
		}

	}

	private bool InternalSetThemeOnUIThread(AppTheme theme)
	{
		var existingIsDark = IsDark;
		var rootElement = RootElement?.XamlRoot?.Content as FrameworkElement;

		if (rootElement is null)
		{
			return false;
		}

		rootElement.RequestedTheme = theme switch
		{
			AppTheme.System => ElementTheme.Default,
			AppTheme.Dark => ElementTheme.Dark,
			AppTheme.Light => ElementTheme.Light,
			_ => ElementTheme.Default,
		};

		SaveDesiredTheme(theme);

		if (existingIsDark != IsDark)
		{
			ThemeChanged?.Invoke(this, theme);
		}
		return true;

	}

	private void SaveDesiredTheme(AppTheme theme)
	{
		try
		{
			_settings.Set(CurrentThemeSettingsKey, theme.ToString());
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
			return Enum.TryParse<AppTheme>(_settings.Get(CurrentThemeSettingsKey) + string.Empty, out var theme) ? theme : AppTheme.System;
		}
		catch (Exception ex)
		{
			if (_logger?.IsEnabled(LogLevel.Error) ?? false) _logger.LogErrorMessage(ex, $"[ThemeService.GetSavedTheme()] - Error while reading stored theme.");
		}

		return AppTheme.System;
	}

	/// <inheritdoc/>
	public async Task InitializeAsync()
	{
		// Allow InitializeAsync to be called multiple times but only
		// do init once
		if (_initialization is not null)
		{
			await _initialization.Task.ConfigureAwait(false);
			return;
		}

		_initialization = new TaskCompletionSource<bool>();

		var theme = GetSavedTheme();
		var success = await InternalSetThemeAsync(theme);
		if (!success)
		{
			if (RootElement is FrameworkElement fe)
			{
				async void OnLoaded(object sender, RoutedEventArgs args)
				{
					fe.Loaded -= OnLoaded;
					var themeSet = await InternalSetThemeAsync(theme);
					CompleteInitialization(themeSet);
				}

				fe.Loaded += OnLoaded;
				await _initialization.Task;
			}
			else
			{
				if (_logger?.IsEnabled(LogLevel.Warning) ?? false) _logger.LogWarningMessage("Unable to attach to Loaded event. Use FrameworkElement instead of UIElement");
				CompleteInitialization(false);
			}
		}
		else
		{
			CompleteInitialization(true);
		}
	}

	private void CompleteInitialization(bool success)
	{
		var init = _initialization;
		if(init is null)
		{
			return;
		}

		init.TrySetResult(success);
	}

	public void Dispose()
	{
		// drop reference, including event handler
		RootElement = null;
	}
}
