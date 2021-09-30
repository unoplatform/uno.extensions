using System.Collections.Generic;

namespace Uno.Extensions.Navigation.Dialogs;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record DialogFactory(IEnumerable<IDialogManager> Dialogs) : IDialogFactory
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public Dialog CreateDialog(INavigationService navigation, NavigationContext context)
    {
        foreach (var dlg in Dialogs)
        {
            if (dlg.IsDialogNavigation(context.Request))
            {
                var dialog = dlg.DisplayDialog(navigation, context, context.ViewModel());
                if (dialog is not null)
                {
                    return dialog;
                }
            }
        }
        return null;
    }

    public bool IsDialogNavigation(NavigationRequest request)
    {
        foreach (var dlg in Dialogs)
        {
            if (dlg.IsDialogNavigation(request))
            {
                return true;
            }
        }

        return false;
    }
}
