using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IFeatureUITestServiceService
{
	ElementFeatureUITest feature
	{
        get;
    }

    Task InitializeAsync();

    Task SetFeatureUITestAsync(ElementFeatureUITest feature);

    Task SetRequestedFeatureUITestAsync();
}
