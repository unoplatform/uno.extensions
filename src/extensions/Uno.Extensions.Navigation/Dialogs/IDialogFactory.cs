namespace Uno.Extensions.Navigation.Dialogs;

public interface IDialogFactory
{
    bool IsDialogNavigation(NavigationRequest request);
    Dialog CreateDialog(INavigationService navigation, NavigationContext context, object viewModel);
}
