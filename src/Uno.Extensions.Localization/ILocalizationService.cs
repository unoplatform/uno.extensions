namespace Uno.Extensions.Localization;

/// <summary>
/// Service for accessing and updating localization information.
/// </summary>
public interface ILocalizationService
{
	/// <summary>
	/// Gets the list of supported cultures (defined in appsettings.json using LocalizationConfiguration section).
	/// </summary>
	CultureInfo[] SupportedCultures { get; }

	/// <summary>
	/// Gets the current culture.
	/// </summary>
	CultureInfo CurrentCulture { get; }

	/// <summary>
	/// Updates the CurrentCulture with a new value.
	/// </summary>
	/// <param name="newCulture">The CultureInfo to set as the new current culture.</param>
	/// <returns>Task to await.</returns>
	Task SetCurrentCultureAsync(CultureInfo newCulture);
}
