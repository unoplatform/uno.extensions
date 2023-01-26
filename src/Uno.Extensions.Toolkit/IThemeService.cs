namespace Uno.Extensions.Toolkit;

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
	event EventHandler<AppTheme> DesiredThemeChanged;
}
