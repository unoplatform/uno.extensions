using Param_RootNamespace.Contracts.Services;
using Param_RootNamespace.Helpers;

using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Services;

public class FeatureUnitTestServiceService : IFeatureUnitTestSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedFeatureUnitTest";

    public ElementFeatureUnitTest FeatureUnitTest { get; set; } = ElementFeatureUnitTest.Default;

    private readonly ILocalSettingsService _localSettingsService;

    public FeatureUnitTestSelectorService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        FeatureUnitTest = await LoadFeatureUnitTestFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetFeatureUnitTestAsync(ElementFeatureUnitTest featureUnitTest)
    {
        FeatureUnitTest = featureUnitTest;

        await SetRequestedFeatureUnitTestAsync();
        await SaveFeatureUnitTestInSettingsAsync(FeatureUnitTest);
    }

    public async Task SetRequestedFeatureUnitTestAsync()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedFeatureUnitTest = FeatureUnitTest;

            TitleBarHelper.UpdateTitleBar(FeatureUnitTest);
        }

        await Task.CompletedTask;
    }

    private async Task<ElementFeatureUnitTest> LoadFeatureUnitTestFromSettingsAsync()
    {
        var featureUnitTestName = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (Enum.TryParse(featureUnitTestName, out ElementFeatureUnitTest cacheFeatureUnitTest))
        {
            return cacheFeatureUnitTest;
        }

        return ElementFeatureUnitTest.Default;
    }

    private async Task SaveFeatureUnitTestInSettingsAsync(ElementFeatureUnitTest featureUnitTest)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, featureUnitTest.ToString());
    }
}
