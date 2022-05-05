using System.Globalization;
using System.Linq;

namespace Uno.Extensions.Localization;

public static class LocalizationSettingsExtensions
{
	public static CultureInfo[] AsCultures(this string[] cultures)
	{
		return cultures.Select(c => c.AsCulture()).ToArray();
	}

	public static CultureInfo AsCulture(this string culture)
	{
		return CultureInfo.GetCultures(CultureTypes.AllCultures)
					.FirstOrDefault(cu => cu.Name == culture);
	}
}
