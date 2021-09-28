namespace Uno.Extensions.Navigation;

public interface INavigationManager 
{
    INavigationRegionService Root { get; }

    INavigationRegionService CreateRegion(object control, object contentControl);
}
