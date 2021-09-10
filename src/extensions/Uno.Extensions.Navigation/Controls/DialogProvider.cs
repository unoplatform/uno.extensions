using System.Collections.Generic;

namespace Uno.Extensions.Navigation.Controls;

public record DialogProvider(IEnumerable<IDialogManager> Dialogs) : IDialogProvider
{
    public Dialog CreateDialog(INavigationService navigation, NavigationContext context, object vm)
    {
        foreach (var dlg in Dialogs)
        {
            var dialog = dlg.DisplayDialog(navigation, context, vm);
            if (dialog is not null)
            {
                return dialog;
            }
        }
        return null;
    }

}
