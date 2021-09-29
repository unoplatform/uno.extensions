namespace Uno.Extensions.Navigation;

public interface INavigationManager 
{
    IRegion Root { get; }

    IRegion CreateService(IRegion parent, params object[] controls);
}
