using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformMobileAndroidSelectorService
{
	ElementPlatformMobileAndroid PlatformMobileAndroid
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformMobileAndroidAsync(ElementPlatformMobileAndroid platformMobileAndroid);

    Task SetRequestedPlatformMobileAndroidAsync();
}
