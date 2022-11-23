using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformDesktopWindowsSelectorService
{
	ElementPlatformDesktopWindows platformDesktopWindows
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformDesktopWindowsAsync(ElementPlatformDesktopWindows platformDesktopWindows);

    Task SetRequestedPlatformAsync();
}
