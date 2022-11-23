using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class PlatformWebWasmSelectorService : IPlatformWebWasmSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedPlatformWebWasm";

    public ElementPlatformWebWasm PlatformWebWasm { get; set; } = ElementPlatformWebWasm.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public PlatformWebWasmSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        PlatformWebWasm = await LoadPlatformWebWasmFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetPlatformWebWasmAsync(ElementPlatformWebWasm platformWebWasm)
    {
        PlatformWebWasm = platformWebWasm;

        await SetRequestedPlatformWebWasmAsync();
        await SavePlatformWebWasmInSettingsAsync(PlatformWebWasm);
    }

    public async Task SetRequestedPlatformWebWasmAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedPlatformWebWasm = PlatformWebWasm;

            TitleBarHelper.UpdateTitleBar(PlatformWebWasm);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementPlatformWebWasm> LoadPlatformWebWasmFromSettingsAsync()
    {
        var platformWebWasmName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(platformWebWasmName, out ElementPlatformWebWasm cachePlatformWebWasm))
        {
            return cachePlatformWebWasm;
        }

        return ElementPlatformWebWasm.Default;
    }

    private async Task SavePlatformWebWasmInSettingsAsync(ElementPlatformWebWasm platformWebWasm)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, platformWebWasm.ToString());
    }
}
