
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Playground.ViewModels;

public class HomeViewModel
{
    public string? Platform { get; }

    public string UseMock { get; }

    private readonly ILocalizationService _localization;
    public HomeViewModel(
        IOptions<Models.AppInfo> appInfo,
        ILocalizationService localization,
        IStringLocalizer localizer)
    {
        _localization = localization;
        Platform = appInfo.Value.Platform;
        SupportedCultures = _localization.SupportedCultures;

        UseMock = (appInfo.Value?.Mock ?? false) ? "Mock ENABLED" : "Mock DISABLED";
    }

    public CultureInfo[] SupportedCultures { get; }

    public CultureInfo SelectedCulture
    {
        get => SupportedCultures.FirstOrDefault(x => x.Name == _localization.CurrentCulture.Name) ?? SupportedCultures.First();
        set
        {
            _ = _localization.SetCurrentCultureAsync(value);
        }
    }

}
