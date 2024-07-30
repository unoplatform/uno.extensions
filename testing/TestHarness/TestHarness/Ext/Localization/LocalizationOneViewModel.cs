using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace TestHarness.Ext.Navigation.Localization;

[ReactiveBindable(false)]
public partial class LocalizationOneViewModel : ObservableObject
{
	private readonly ILocalizationService _localizationService;
	
	[ObservableProperty]
	private string _applicationNameInCode;

	[ObservableProperty]
	private string _keyWithDots;

	public LocalizationOneViewModel(
		ILocalizationService localizationService,
		IStringLocalizer localizer)
	{
		_localizationService = localizationService;
		SupportedCultures = _localizationService.SupportedCultures;

		ApplicationNameInCode = localizer.GetString("ApplicationName");
		KeyWithDots = localizer.GetString("Key.With.Dots");
	}

	public CultureInfo[] SupportedCultures { get; }

	public CultureInfo SelectedCulture
	{
		get => SupportedCultures.FirstOrDefault(x => x.Name == _localizationService.CurrentCulture.Name) ?? SupportedCultures.First();
		set
		{
			_ = _localizationService.SetCurrentCultureAsync(value);
		}
	}

}

