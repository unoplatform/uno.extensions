using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IFeaturePWAServiceService
{
	ElementFeaturePWA feature
	{
        get;
    }

    Task InitializeAsync();

    Task SetFeaturePWAAsync(ElementFeaturePWA feature);

    Task SetRequestedFeaturePWAAsync();
}
