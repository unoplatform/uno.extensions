using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformDesktopWindowsWPFSelectorService : IPlatformDesktopWindowsWPFSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatformDesktopWindowsWPF";

    public ElementPlatformDesktopWindowsWPF PlatformDesktopWindowsWPF { get; set; } = ElementPlatformDesktopWindowsWPF.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformDesktopWindowsWPFSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        PlatformDesktopWindowsWPF = await LoadPlatformDesktopWindowsWPFFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformDesktopWindowsWPFAsync(ElementPlatformDesktopWindowsWPF platformDesktopWindowsWPF)
    {
        PlatformDesktopWindowsWPF = platformDesktopWindowsWPF;

        await SetRequestedPlatformDesktopWindowsWPFAsync();
        await SavePlatformDesktopWindowsWPFInSettingsAsync(PlatformDesktopWindowsWPF);
    }

    public async Task SetRequestedPlatformDesktopWindowsWPFAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatformDesktopWindowsWPF = PlatformDesktopWindowsWPF;

            TitleBarHelper.UpdateTitleBar(PlatformDesktopWindowsWPF);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatformDesktopWindowsWPF> LoadPlatformDesktopWindowsWPFFromSettingsAsync()
    {
        var platformDesktopWindowsWPFName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformDesktopWindowsWPFName, out ElementPlatformDesktopWindowsWPF cachePlatformDesktopWindowsWPF))
        {
            return cachePlatformDesktopWindowsWPF;
        }

        return ElementPlatformDesktopWindowsWPF.Default;
    }

    private async Task SavePlatformDesktopWindowsWPFInSettingsAsync(ElementPlatformDesktopWindowsWPF platformDesktopWindowsWPF)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platformDesktopWindowsWPF.ToString());
    }
}
