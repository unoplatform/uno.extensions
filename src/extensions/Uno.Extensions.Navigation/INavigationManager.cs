namespace Uno.Extensions.Navigation;

public interface INavigationManager 
{
    INavigationRegionContainer Root { get; }

    INavigationRegionContainer CreateRegion(object control, object contentControl);

    //INavigationService AddRegion(INavigationService parentRegion, string regionName, object control, INavigationService existingRegion);

    //void RemoveRegion(INavigationService region);
}
