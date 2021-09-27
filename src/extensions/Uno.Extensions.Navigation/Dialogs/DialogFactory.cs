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
            var dialog = dlg.DisplayDialog(navigation, context, context.ViewModel());
            if (dialog is not null)
            {
                return dialog;
            }
        }
        return null;
    }

}
