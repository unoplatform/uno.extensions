namespace Uno.Extensions.Navigation;

public interface INavigationManager 
{
    INavigationService Root { get; }

    INavigationService CreateService(INavigationService parent, params object[] controls);
}
