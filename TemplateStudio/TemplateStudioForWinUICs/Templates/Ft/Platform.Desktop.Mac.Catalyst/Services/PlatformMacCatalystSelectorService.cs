using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformMacCatalystSelectorService : IPlatformMacCatalystSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatformMacCatalyst";

    public ElementPlatformMacCatalyst PlatformMacCatalyst { get; set; } = ElementPlatformMacCatalyst.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformMacCatalystSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        PlatformMacCatalyst = await LoadPlatformMacCatalystFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformMacCatalystAsync(ElementPlatformMacCatalyst platformMacCatalyst)
    {
        PlatformMacCatalyst = platformMacCatalyst;

        await SetRequestedPlatformMacCatalystAsync();
        await SavePlatformMacCatalystInSettingsAsync(PlatformMacCatalyst);
    }

    public async Task SetRequestedPlatformMacCatalystAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatformMacCatalyst = PlatformMacCatalyst;

            TitleBarHelper.UpdateTitleBar(PlatformMacCatalyst);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatformMacCatalyst> LoadPlatformMacCatalystFromSettingsAsync()
    {
        var platformMacCatalystName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformMacCatalystName, out ElementPlatformMacCatalyst cachePlatformMacCatalyst))
        {
            return cachePlatformMacCatalyst;
        }

        return ElementPlatformMacCatalyst.Default;
    }

    private async Task SavePlatformMacCatalystInSettingsAsync(ElementPlatformMacCatalyst platformMacCatalyst)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platformMacCatalyst.ToString());
    }
}
