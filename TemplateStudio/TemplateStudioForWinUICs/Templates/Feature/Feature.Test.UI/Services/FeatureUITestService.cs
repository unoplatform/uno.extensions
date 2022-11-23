using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class FeatureUITestServiceService : IFeatureUITestSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedFeatureUITest";

    public ElementFeatureUITest FeatureUITest { get; set; } = ElementFeatureUITest.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public FeatureUITestSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        FeatureUITest = await LoadFeatureUITestFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetFeatureUITestAsync(ElementFeatureUITest featureUITest)
    {
        FeatureUITest = featureUITest;

        await SetRequestedFeatureUITestAsync();
        await SaveFeatureUITestInSettingsAsync(FeatureUITest);
    }

    public async Task SetRequestedFeatureUITestAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedFeatureUITest = FeatureUITest;

            TitleBarHelper.UpdateTitleBar(FeatureUITest);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementFeatureUITest> LoadFeatureUITestFromSettingsAsync()
    {
        var featureUITestName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(featureUITestName, out ElementFeatureUITest cacheFeatureUITest))
        {
            return cacheFeatureUITest;
        }

        return ElementFeatureUITest.Default;
    }

    private async Task SaveFeatureUITestInSettingsAsync(ElementFeatureUITest featureUITest)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, featureUITest.ToString());
    }
}
