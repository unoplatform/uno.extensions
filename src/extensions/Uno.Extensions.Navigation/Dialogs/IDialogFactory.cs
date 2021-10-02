namespace Uno.Extensions.Navigation.Dialogs;

public interface IDialogFactory
{
    bool IsDialogNavigation(NavigationRequest request);

    Dialog CreateDialog(NavigationContext context, object viewModel);
}
