
using Microsoft.Extensions.Localization;
using Uno.Extensions.Localization;

namespace Playground.ViewModels;

public class HomeViewModel
{
	public string? Platform { get; }

	private readonly IWritableOptions<LocalizationSettings> _localization;
	private readonly IStringLocalizer _localizer;
	public HomeViewModel(
		IOptions<AppInfo> appInfo,
		IWritableOptions<LocalizationSettings> localization,
		IStringLocalizer localizer)
	{
		_localization = localization;
		Platform = appInfo.Value.Platform;

		var language = localizer[_localization.Value.CurrentCulture ?? "en"];
	}

	public string[] SupportedCultures => _localization.Value?.Cultures ?? new[] { "en-US" };

	public string SelectedCulture {
		get => _localization.Value?.CurrentCulture?? _localization.Value?.Cultures?.FirstOrDefault()??"en-US";
		set => _localization.Update(settings => settings.CurrentCulture = value);
	}

}
