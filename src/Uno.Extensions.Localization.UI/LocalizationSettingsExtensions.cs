namespace Uno.Extensions.Localization;

/// <summary>
/// Contains extension methods to use with <see cref="LocalizationSettings"/>.
/// </summary>
public static class LocalizationSettingsExtensions
{
	/// <summary>
	/// Converts an array of culture names to an array of CultureInfo objects.
	/// </summary>
	/// <param name="cultures">
	/// The array of culture names to convert.
	/// </param>
	/// <returns>
	/// An array of CultureInfo objects.
	/// </returns>
	public static CultureInfo[] AsCultures(this string[] cultures)
	{
		return (from c in cultures.Distinct()
				let cult = c.AsCulture()
				where cult is not null
				select cult).ToArray();
	}

	/// <summary>
	/// Converts a culture name to a CultureInfo object.
	/// </summary>
	/// <param name="culture">
	/// The culture name to convert.
	/// </param>
	/// <returns>
	/// A CultureInfo object.
	/// </returns>
	public static CultureInfo? AsCulture(this string culture)
	{
		return CultureInfo.GetCultures(CultureTypes.AllCultures)
					.FirstOrDefault(cu => cu.Name == culture);
	}
}
