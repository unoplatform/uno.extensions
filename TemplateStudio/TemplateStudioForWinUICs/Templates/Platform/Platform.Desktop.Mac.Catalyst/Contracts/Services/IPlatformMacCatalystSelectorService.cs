using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformMacCatalystSelectorService
{
	ElementPlatformMacCatalyst PlatformMacCatalyst
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformMacCatalystAsync(ElementPlatformMacCatalyst platformMacCatalyst);

    Task SetRequestedPlatformMacCatalystAsync();
}
