using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformDesktopWindowsWPFSelectorService
{
	ElementPlatformDesktopWindowsWPF PlatformDesktopWindowsWPF
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformDesktopWindowsWPFAsync(ElementPlatformDesktopWindowsWPF platformDesktopWindowsWPF);

    Task SetRequestedPlatformDesktopWindowsWPFAsync();
}
