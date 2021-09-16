namespace Uno.Extensions.Navigation;

public interface INavigationManager : INavigationService
{
    INavigationService AddRegion(INavigationService parentRegion, string regionName, object control, INavigationService existingRegion);

    void RemoveRegion(INavigationService region);
}
