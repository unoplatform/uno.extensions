namespace Uno.Extensions.Navigation;

public interface IRegionNavigationServiceFactory
{
    IRegionNavigationService Root { get; }

    IRegionNavigationService CreateService(object control, bool isComposite);
}
