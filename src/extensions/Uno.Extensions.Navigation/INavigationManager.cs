namespace Uno.Extensions.Navigation;

public interface INavigationManager 
{
    IRegionNavigationService Root { get; }

    IRegionNavigationService CreateService(IRegionNavigationService parent, params object[] controls);
}
