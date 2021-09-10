namespace Uno.Extensions.Navigation.Controls;

public interface IDialogManager
{
    object CloseDialog(Dialog dialog, NavigationContext context, object responseData);

    Dialog DisplayDialog(INavigationService navigation, NavigationContext context, object vm);
}
