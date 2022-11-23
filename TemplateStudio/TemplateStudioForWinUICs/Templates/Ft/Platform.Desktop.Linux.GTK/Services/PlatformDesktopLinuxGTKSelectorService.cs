using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformDesktopLinuxGTKSelectorService : IPlatformDesktopLinuxGTKSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatformDesktopLinuxGTK";

    public ElementPlatformDesktopLinuxGTK PlatformDesktopLinuxGTK { get; set; } = ElementPlatformDesktopLinuxGTK.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformDesktopLinuxGTKSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        PlatformDesktopLinuxGTK = await LoadPlatformDesktopLinuxGTKFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformDesktopLinuxGTKAsync(ElementPlatformDesktopLinuxGTK platformDesktopLinuxGTK)
    {
        PlatformDesktopLinuxGTK = platformDesktopLinuxGTK;

        await SetRequestedPlatformDesktopLinuxGTKAsync();
        await SavePlatformDesktopLinuxGTKInSettingsAsync(PlatformDesktopLinuxGTK);
    }

    public async Task SetRequestedPlatformDesktopLinuxGTKAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatformDesktopLinuxGTK = PlatformDesktopLinuxGTK;

            TitleBarHelper.UpdateTitleBar(PlatformDesktopLinuxGTK);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatformDesktopLinuxGTK> LoadPlatformDesktopLinuxGTKFromSettingsAsync()
    {
        var platformDesktopLinuxGTKName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformDesktopLinuxGTKName, out ElementPlatformDesktopLinuxGTK cachePlatformDesktopLinuxGTK))
        {
            return cachePlatformDesktopLinuxGTK;
        }

        return ElementPlatformDesktopLinuxGTK.Default;
    }

    private async Task SavePlatformDesktopLinuxGTKInSettingsAsync(ElementPlatformDesktopLinuxGTK platformDesktopLinuxGTK)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platformDesktopLinuxGTK.ToString());
    }
}
