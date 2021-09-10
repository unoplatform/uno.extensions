namespace Uno.Extensions.Navigation.Controls;

public interface IDialogProvider
{
    Dialog CreateDialog(INavigationService navigation, NavigationContext context, object vm);
}
