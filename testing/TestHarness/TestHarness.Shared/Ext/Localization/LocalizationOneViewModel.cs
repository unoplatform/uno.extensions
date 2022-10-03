using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace TestHarness.Ext.Navigation.Localization;

[ReactiveBindable(false)]
public partial class LocalizationOneViewModel : ObservableObject
{
	private readonly ILocalizationService _localizationService;
	public LocalizationOneViewModel(
		ILocalizationService localizationService,
		IStringLocalizer localizer)
	{
		_localizationService = localizationService;
		SupportedCultures = _localizationService.SupportedCultures;

		var language = localizer[_localizationService.CurrentCulture.Name ?? "en"];
	}

	public CultureInfo[] SupportedCultures { get; }

	public CultureInfo SelectedCulture
	{
		get => SupportedCultures.FirstOrDefault(x => x.Name == _localizationService.CurrentCulture.Name) ?? SupportedCultures.First();
		set
		{
			_ = _localizationService.UpdateCurrentCulture(value);
		}
	}

}

