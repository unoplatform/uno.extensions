namespace Uno.Extensions.Toolkit;

/// <summary>
/// Platform agnostic abstraction for light/dark theme
/// </summary>
public enum AppTheme
{
	/// <summary>
	/// Use the system theme
	/// </summary>
	System = default,

	/// <summary>
	/// Use the light theme
	/// </summary>
	Light = 1,

	/// <summary>
	/// Use the dark theme
	/// </summary>
	Dark = 2
}
