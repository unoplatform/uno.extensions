using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformSelectorService : IPlatformSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatform";

    public ElementPlatform Platform { get; set; } = ElementPlatform.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        Platform = await LoadPlatformFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformAsync(ElementPlatform platform)
    {
        Platform = platform;

        await SetRequestedPlatformAsync();
        await SavePlatformInSettingsAsync(Platform);
    }

    public async Task SetRequestedPlatformAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatform = Platform;

            TitleBarHelper.UpdateTitleBar(Platform);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatform> LoadPlatformFromSettingsAsync()
    {
        var platformName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformName, out ElementPlatform cachePlatform))
        {
            return cachePlatform;
        }

        return ElementPlatform.Default;
    }

    private async Task SavePlatformInSettingsAsync(ElementPlatform platform)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platform.ToString());
    }
}
