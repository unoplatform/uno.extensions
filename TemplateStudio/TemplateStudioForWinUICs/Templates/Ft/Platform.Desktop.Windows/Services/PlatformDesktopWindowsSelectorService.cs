using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformDesktopWindowsSelectorService : IPlatformDesktopWindowsDesktopWindowsSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatformDesktopWindows";

    public ElementPlatformDesktopWindows PlatformDesktopWindows { get; set; } = ElementPlatformDesktopWindows.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformDesktopWindowsSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        PlatformDesktopWindows = await LoadPlatformDesktopWindowsFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformDesktopWindowsAsync(ElementPlatformDesktopWindows platformDesktopWindows)
    {
        PlatformDesktopWindows = platformDesktopWindows;

        await SetRequestedPlatformDesktopWindowsAsync();
        await SavePlatformDesktopWindowsInSettingsAsync(PlatformDesktopWindows);
    }

    public async Task SetRequestedPlatformDesktopWindowsAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatformDesktopWindows = PlatformDesktopWindows;

            TitleBarHelper.UpdateTitleBar(PlatformDesktopWindows);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatformDesktopWindows> LoadPlatformDesktopWindowsFromSettingsAsync()
    {
        var platformDesktopWindowsName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformDesktopWindowsName, out ElementPlatformDesktopWindows cachePlatformDesktopWindows))
        {
            return cachePlatformDesktopWindows;
        }

        return ElementPlatformDesktopWindows.Default;
    }

    private async Task SavePlatformDesktopWindowsInSettingsAsync(ElementPlatformDesktopWindows platformDesktopWindows)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platformDesktopWindows.ToString());
    }
}
