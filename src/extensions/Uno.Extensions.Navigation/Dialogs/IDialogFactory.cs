namespace Uno.Extensions.Navigation.Dialogs;

public interface IDialogFactory
{
    Dialog CreateDialog(INavigationService navigation, NavigationContext context, object vm);
}
