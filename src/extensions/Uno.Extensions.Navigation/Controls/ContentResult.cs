#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls;

public record ContentResult(ContentDialogResult Result, object Data = null)
{
    public static implicit operator ContentDialogResult(
                                   ContentResult entity)
    {
        return entity.Result;
    }
}
