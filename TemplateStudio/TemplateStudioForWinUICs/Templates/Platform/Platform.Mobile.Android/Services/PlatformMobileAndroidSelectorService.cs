using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformMobileAndroidSelectorService : IPlatformMobileAndroidSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatformMobileAndroid";

    public ElementPlatformMobileAndroid PlatformMobileAndroid { get; set; } = ElementPlatformMobileAndroid.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformMobileAndroidSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        PlatformMobileAndroid = await LoadPlatformMobileAndroidFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformMobileAndroidAsync(ElementPlatformMobileAndroid platformMobileAndroid)
    {
        PlatformMobileAndroid = platformMobileAndroid;

        await SetRequestedPlatformMobileAndroidAsync();
        await SavePlatformMobileAndroidInSettingsAsync(PlatformMobileAndroid);
    }

    public async Task SetRequestedPlatformMobileAndroidAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatformMobileAndroid = PlatformMobileAndroid;

            TitleBarHelper.UpdateTitleBar(PlatformMobileAndroid);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatformMobileAndroid> LoadPlatformMobileAndroidFromSettingsAsync()
    {
        var platformMobileAndroidName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformMobileAndroidName, out ElementPlatformMobileAndroid cachePlatformMobileAndroid))
        {
            return cachePlatformMobileAndroid;
        }

        return ElementPlatformMobileAndroid.Default;
    }

    private async Task SavePlatformMobileAndroidInSettingsAsync(ElementPlatformMobileAndroid platformMobileAndroid)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platformMobileAndroid.ToString());
    }
}
