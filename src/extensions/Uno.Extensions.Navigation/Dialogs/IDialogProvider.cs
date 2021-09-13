namespace Uno.Extensions.Navigation.Dialogs;

public interface IDialogProvider
{
    Dialog CreateDialog(INavigationService navigation, NavigationContext context, object vm);
}
