using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace TestHarness.Ext.Navigation.Localization;

[ReactiveBindable(false)]
public partial class LocalizationOneViewModel : ObservableObject
{
	private readonly IWritableOptions<LocalizationSettings> _localization;
	public LocalizationOneViewModel(
		IOptions<LocalizationConfiguration> configuration,
		IWritableOptions<LocalizationSettings> localization,
		IStringLocalizer localizer)
	{
		_localization = localization;
		SupportedCultures = configuration.Value?.Cultures?.AsCultures() ?? new[] { "en-US".AsCulture()! };

		var language = localizer[_localization.Value?.CurrentCulture ?? "en"];
	}

	public CultureInfo[] SupportedCultures { get; }

	public CultureInfo SelectedCulture
	{
		get => SupportedCultures.FirstOrDefault(x => x.Name == _localization.Value?.CurrentCulture) ?? SupportedCultures.First();
		set
		{
			_ = _localization.UpdateAsync(settings => settings with { CurrentCulture = value.Name });
		}
	}

}

