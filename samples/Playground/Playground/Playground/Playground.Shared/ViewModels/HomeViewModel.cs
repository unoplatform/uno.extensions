
using System.Globalization;
using Microsoft.Extensions.Localization;
using Uno.Extensions.Localization;

namespace Playground.ViewModels;

public class HomeViewModel
{
	public string? Platform { get; }

	public string UseMock { get; }

	private readonly IWritableOptions<LocalizationSettings> _localization;
	public HomeViewModel(
		IOptions<AppInfo> appInfo,
		IWritableOptions<LocalizationSettings> localization,
		IStringLocalizer localizer)
	{
		_localization = localization;
		Platform = appInfo.Value.Platform;
		SupportedCultures = _localization.Value?.Cultures?.AsCultures() ?? new[] { "en-US".AsCulture() }; 

		var language = localizer[_localization.Value?.CurrentCulture ?? "en"];

		UseMock = (appInfo.Value?.Mock ?? false) ? "Mock ENABLED" : "Mock DISABLED";
	}

	public CultureInfo[] SupportedCultures { get; }

	public CultureInfo SelectedCulture {
		get => SupportedCultures.FirstOrDefault(x=>x.Name == _localization.Value?.CurrentCulture)?? SupportedCultures.First();
		set => _localization.Update(settings => settings.CurrentCulture = value.Name);
	}

}
