namespace Uno.Extensions.Navigation.Dialogs;

public interface IDialogManager
{
    object CloseDialog(Dialog dialog, NavigationContext context, object responseData);

    Dialog DisplayDialog(INavigationService navigation, NavigationContext context, object vm);
}
