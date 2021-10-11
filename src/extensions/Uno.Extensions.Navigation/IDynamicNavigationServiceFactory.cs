namespace Uno.Extensions.Navigation;

public interface IDynamicNavigationServiceFactory
{
    IRegionNavigationService CreateService(NavigationRequest request);
}
