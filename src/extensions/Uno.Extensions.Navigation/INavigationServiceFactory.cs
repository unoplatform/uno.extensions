namespace Uno.Extensions.Navigation;

public interface INavigationServiceFactory
{
    IRegionNavigationService Root { get; }

    IRegionNavigationService CreateService(object control, bool isComposite);
}
