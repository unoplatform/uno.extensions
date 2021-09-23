namespace Uno.Extensions.Navigation;

public interface INavigationManager 
{
    INavigationService Root { get; }

    INavigationService AddRegion(INavigationService parentRegion, string regionName, object control, INavigationService existingRegion);

    void RemoveRegion(INavigationService region);
}
