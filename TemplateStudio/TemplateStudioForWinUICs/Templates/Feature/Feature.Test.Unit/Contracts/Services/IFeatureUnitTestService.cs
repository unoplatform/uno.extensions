using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IFeatureUnitTestServiceService
{
	ElementFeatureUnitTest FeatureUnitTest
	{
        get;
    }

    Task InitializeAsync();

    Task SetFeatureUnitTestAsync(ElementFeatureUnitTest FeatureUnitTest);

    Task SetRequestedFeatureUnitTestAsync();
}
