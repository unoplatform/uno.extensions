using System.Globalization;
using System.Linq;

namespace Uno.Extensions.Localization;

public static class LocalizationSettingsExtensions
{
	public static CultureInfo[] AsCultures(this string[] cultures)
	{
		return (from c in cultures
				let cult = c.AsCulture()
				where cult is not null
				select cult).ToArray();
	}

	public static CultureInfo? AsCulture(this string culture)
	{
		return CultureInfo.GetCultures(CultureTypes.AllCultures)
					.FirstOrDefault(cu => cu.Name == culture);
	}
}
