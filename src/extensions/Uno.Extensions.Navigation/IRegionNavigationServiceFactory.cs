namespace Uno.Extensions.Navigation;

public interface IRegionNavigationServiceFactory
{
    IRegionNavigationService CreateService(object control, bool isComposite);
}
