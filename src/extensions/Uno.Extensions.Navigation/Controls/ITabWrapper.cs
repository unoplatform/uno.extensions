using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Threading;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
#else
using Windows.UI.Popups;
using UICommand = Windows.UI.Popups.UICommand;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls;

public interface ITabWrapper : IControlNavigation
{
    string CurrentTabName { get; }

    bool ContainsTab(string tabName);
}
