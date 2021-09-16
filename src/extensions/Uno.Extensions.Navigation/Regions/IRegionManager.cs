namespace Uno.Extensions.Navigation.Regions;

public interface IRegionManager : INavigationAware
{
    bool IsCurrentPath(string path);

    NavigationResponse Navigate(NavigationContext context);
}
