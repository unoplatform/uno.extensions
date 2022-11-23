using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformMacMacOSSelectorService : IPlatformMacMacOSSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatformMacMacOS";

    public ElementPlatformMacMacOS PlatformMacMacOS { get; set; } = ElementPlatformMacMacOS.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformMacMacOSSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        PlatformMacMacOS = await LoadPlatformMacMacOSFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformMacMacOSAsync(ElementPlatformMacMacOS platformMacMacOS)
    {
        PlatformMacMacOS = platformMacMacOS;

        await SetRequestedPlatformMacMacOSAsync();
        await SavePlatformMacMacOSInSettingsAsync(PlatformMacMacOS);
    }

    public async Task SetRequestedPlatformMacMacOSAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatformMacMacOS = PlatformMacMacOS;

            TitleBarHelper.UpdateTitleBar(PlatformMacMacOS);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatformMacMacOS> LoadPlatformMacMacOSFromSettingsAsync()
    {
        var platformMacMacOSName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformMacMacOSName, out ElementPlatformMacMacOS cachePlatformMacMacOS))
        {
            return cachePlatformMacMacOS;
        }

        return ElementPlatformMacMacOS.Default;
    }

    private async Task SavePlatformMacMacOSInSettingsAsync(ElementPlatformMacMacOS platformMacMacOS)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platformMacMacOS.ToString());
    }
}
