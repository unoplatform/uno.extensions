using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformMacMacOSSelectorService
{
	ElementPlatformMacMacOS PlatformMacMacOS
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformMacMacOSAsync(ElementPlatformMacMacOS platformMacMacOS);

    Task SetRequestedPlatformMacMacOSAsync();
}
