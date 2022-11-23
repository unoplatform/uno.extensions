using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformMobileIOSSelectorService : IPlatformMobileIOSSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatformMobileIOS";

    public ElementPlatformMobileIOS PlatformMobileIOS { get; set; } = ElementPlatformMobileIOS.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformMobileIOSSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        PlatformMobileIOS = await LoadPlatformMobileIOSFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformMobileIOSAsync(ElementPlatformMobileIOS platformMobileIOS)
    {
        PlatformMobileIOS = platformMobileIOS;

        await SetRequestedPlatformMobileIOSAsync();
        await SavePlatformMobileIOSInSettingsAsync(PlatformMobileIOS);
    }

    public async Task SetRequestedPlatformMobileIOSAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatformMobileIOS = PlatformMobileIOS;

            TitleBarHelper.UpdateTitleBar(PlatformMobileIOS);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatformMobileIOS> LoadPlatformMobileIOSFromSettingsAsync()
    {
        var platformMobileIOSName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformMobileIOSName, out ElementPlatformMobileIOS cachePlatformMobileIOS))
        {
            return cachePlatformMobileIOS;
        }

        return ElementPlatformMobileIOS.Default;
    }

    private async Task SavePlatformMobileIOSInSettingsAsync(ElementPlatformMobileIOS platformMobileIOS)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platformMobileIOS.ToString());
    }
}
