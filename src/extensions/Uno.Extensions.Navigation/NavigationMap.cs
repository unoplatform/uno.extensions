using System;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
#endif

namespace Uno.Extensions.Navigation
{
    public record NavigationMap(
        string Path,
        Type View,
        Type ViewModel = null,
        Type Data = null)
    { }

}
