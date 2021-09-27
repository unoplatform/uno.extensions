namespace Uno.Extensions.Navigation;

public interface INavigationManager 
{
    INavigationRegionContainer Root { get; }

    INavigationRegionContainer CreateRegion(object control, object contentControl);
}
