namespace Uno.Extensions.Navigation;

public interface IDialogNavigationServiceFactory
{
    IRegionNavigationService CreateService(NavigationRequest request);
}
