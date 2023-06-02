namespace Uno.Extensions.Toolkit;

/// <summary>
/// Abstraction for controlling the theme of the application
/// </summary>
public interface IThemeService
{
	/// <summary>
	/// Get if the application is currently in dark mode.
	/// </summary>
	bool IsDark { get; }

	/// <summary>
	///  Get the previously saved theme.
	/// </summary>
	AppTheme Theme { get; }

	/// <summary>
	/// Sets the system theme for the provided XamlRoot.
	/// </summary>
	/// <returns>Indicates success; Fails if no XamlRoot found</returns>
	Task<bool> SetThemeAsync(AppTheme theme);

	/// <summary>
	/// Event that fires up whenever SetThemeAsync() is called.
	/// </summary>
	event EventHandler<AppTheme> ThemeChanged;

	/// <summary>
	/// Initializes the theme service - this is triggered
	/// automatically when Theme Service is constructed but
	/// can be invoked manually in order to await Initialise
	/// completion
	/// </summary>
	/// <returns>Task that can be awaited for initialize complete</returns>
	Task InitializeAsync();
}
