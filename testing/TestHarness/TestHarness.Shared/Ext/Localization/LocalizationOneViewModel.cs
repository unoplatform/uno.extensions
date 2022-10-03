using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace TestHarness.Ext.Navigation.Localization;

[ReactiveBindable(false)]
public partial class LocalizationOneViewModel : ObservableObject
{
	private readonly IWritableOptions<LocalizationSettings> _localization;
	private readonly ILocalizationService _localizationService;
	public LocalizationOneViewModel(
		ILocalizationService localizationService,
		IOptions<LocalizationConfiguration> configuration,
		IWritableOptions<LocalizationSettings> localization,
		IStringLocalizer localizer)
	{
		_localizationService = localizationService;
		_localization = localization;
		SupportedCultures = _localizationService.SupportedCultures;

			//configuration.Value?.Cultures?.AsCultures() ?? new[] { "en-US".AsCulture()! };

		var language = localizer[_localizationService.CurrentCulture.Name ?? "en"];
	}

	public CultureInfo[] SupportedCultures { get; }

	public CultureInfo SelectedCulture
	{
		get => SupportedCultures.FirstOrDefault(x => x.Name == _localizationService.CurrentCulture.Name) ?? SupportedCultures.First();
		set
		{
			_ = _localizationService.UpdateCurrentCulture(value);
//			_ = _localization.UpdateAsync(settings => settings with { CurrentCulture = value.Name });
		}
	}

}

