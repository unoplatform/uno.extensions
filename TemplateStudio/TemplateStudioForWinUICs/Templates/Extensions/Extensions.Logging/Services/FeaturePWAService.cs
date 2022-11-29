using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class FeaturePWAServiceService : IFeaturePWAServiceSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedFeaturePWAService";

    public ElementFeaturePWAService FeaturePWAService { get; set; } = ElementFeaturePWAService.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public FeaturePWAServiceSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        FeaturePWAService = await LoadFeaturePWAServiceFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetFeaturePWAServiceAsync(ElementFeaturePWAService featurePWAService)
    {
        FeaturePWAService = featurePWAService;

        await SetRequestedFeaturePWAServiceAsync();
        await SaveFeaturePWAServiceInSettingsAsync(FeaturePWAService);
    }

    public async Task SetRequestedFeaturePWAServiceAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedFeaturePWAService = FeaturePWAService;

            TitleBarHelper.UpdateTitleBar(FeaturePWAService);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementFeaturePWAService> LoadFeaturePWAServiceFromSettingsAsync()
    {
        var featurePWAServiceName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(featurePWAServiceName, out ElementFeaturePWAService cacheFeaturePWAService))
        {
            return cacheFeaturePWAService;
        }

        return ElementFeaturePWAService.Default;
    }

    private async Task SaveFeaturePWAServiceInSettingsAsync(ElementFeaturePWAService featurePWAService)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, featurePWAService.ToString());
    }
}
