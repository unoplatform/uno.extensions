namespace Uno.Extensions.Navigation;

public interface INavigationManager 
{
    IRegionService Root { get; }

    IRegionService CreateService(IRegionService parent, params object[] controls);
}
