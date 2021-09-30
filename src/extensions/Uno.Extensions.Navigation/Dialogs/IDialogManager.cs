namespace Uno.Extensions.Navigation.Dialogs;

public interface IDialogManager
{
    bool IsDialogNavigation(NavigationRequest context);

    object CloseDialog(Dialog dialog, NavigationContext context, object responseData);

    Dialog DisplayDialog(INavigationService navigation, NavigationContext context, object vm);
}
