namespace Uno.Extensions.Navigation;

public interface INavigationManager 
{
    INavigationService Root { get; }

    INavigationService CreateService(object control, object contentControl);
}
