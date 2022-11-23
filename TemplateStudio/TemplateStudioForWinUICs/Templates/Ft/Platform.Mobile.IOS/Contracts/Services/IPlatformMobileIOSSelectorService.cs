using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformSelectorService
{
	ElementPlatform Platform
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformAsync(ElementPlatform platformMobileIOS);

    Task SetRequestedPlatformAsync();
}
