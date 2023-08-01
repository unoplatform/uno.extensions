namespace Uno.Extensions.Localization;

/// <summary>
/// Contains configuration for the localization feature.
/// </summary>
public record LocalizationConfiguration
{
	/// <summary>
	/// An array of valid CultureInfo names which represent cultures supported 
	/// by the localization service. The elements are not case-sensitive.
	/// </summary>
	public string[]? Cultures { get; init; }
}
