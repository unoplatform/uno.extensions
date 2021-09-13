#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Dialogs;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record ContentResult(ContentDialogResult Result, object Data = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public static implicit operator ContentDialogResult(
                                   ContentResult entity)
    {
        return entity.Result;
    }
}
