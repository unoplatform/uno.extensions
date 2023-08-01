namespace Uno.Extensions.Localization;

/// <summary>
/// Contains settings for the localization feature.
/// </summary>
public record LocalizationSettings
{
	/// <summary>
	/// The desired culture to use for localization.
	/// </summary>
	public string? CurrentCulture { get; set; }
}
