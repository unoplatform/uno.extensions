namespace Uno.Extensions.Navigation;

public interface INavigationServiceFactory
{
    IRegionNavigationService Root { get; }

    IRegionNavigationService CreateService(IRegionNavigationService parent, params object[] controls);
}
