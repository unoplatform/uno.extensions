using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformDesktopLinuxGTKDesktopLinuxGTKSelectorService
{
	ElementPlatformDesktopLinuxGTK PlatformDesktopLinuxGTK
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformDesktopLinuxGTKAsync(ElementPlatformDesktopLinuxGTK platformDesktopLinuxGTK);

    Task SetRequestedPlatformDesktopLinuxGTKAsync();
}
