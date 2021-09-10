using System;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#else
#endif

namespace Uno.Extensions.Navigation.Adapters
{
    public interface IAdapterFactory
    {
        Type ControlType { get; }
        INavigationAdapter Create();
    }
}
