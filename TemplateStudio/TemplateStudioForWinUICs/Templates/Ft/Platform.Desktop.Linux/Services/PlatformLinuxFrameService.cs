using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformWindowsFrameSelectorService : IPlatformWindowsFrameSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatformWindowsFrame";

    public ElementPlatformWindowsFrame PlatformWindowsFrame { get; set; } = ElementPlatformWindowsFrame.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformWindowsFrameSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        PlatformWindowsFrame = await LoadPlatformWindowsFrameFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformWindowsFrameAsync(ElementPlatformWindowsFrame platformWindowsFrame)
    {
        PlatformWindowsFrame = platformWindowsFrame;

        await SetRequestedPlatformWindowsFrameAsync();
        await SavePlatformWindowsFrameInSettingsAsync(PlatformWindowsFrame);
    }

    public async Task SetRequestedPlatformWindowsFrameAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatformWindowsFrame = PlatformWindowsFrame;

            TitleBarHelper.UpdateTitleBar(PlatformWindowsFrame);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatformWindowsFrame> LoadPlatformWindowsFrameFromSettingsAsync()
    {
        var platformWindowsFrameName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformWindowsFrameName, out ElementPlatformWindowsFrame cachePlatformWindowsFrame))
        {
            return cachePlatformWindowsFrame;
        }

        return ElementPlatformWindowsFrame.Default;
    }

    private async Task SavePlatformWindowsFrameInSettingsAsync(ElementPlatformWindowsFrame platformWindowsFrame)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platformWindowsFrame.ToString());
    }
}
