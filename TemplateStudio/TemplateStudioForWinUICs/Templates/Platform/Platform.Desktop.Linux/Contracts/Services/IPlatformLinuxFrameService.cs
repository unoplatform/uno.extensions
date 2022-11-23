using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformLinuxFrameService
{
	ElementPlatformLinuxFrame platformLinuxFrame
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformLinuxFrameAsync(ElementPlatformLinuxFrame platformLinuxFrame);

    Task SetRequestedPlatformLinuxFrameAsync();
}
