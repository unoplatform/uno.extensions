namespace Uno.Extensions.Navigation;

public interface IRegionNavigationServiceFactory
{
    IRegionNavigationService CreateService(IRegionNavigationService parent, object control, bool isComposite);

    IRegionNavigationService CreateService(NavigationRequest request);
}
