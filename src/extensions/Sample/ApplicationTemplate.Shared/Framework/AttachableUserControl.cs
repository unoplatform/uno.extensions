using System;
using System.Collections.Generic;
using System.Text;
//-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace ApplicationTemplate.Views
{
    /// <summary>
    /// This is a workaround the fact that using attached properties on UserControl doesn't work with Uno.UI
    /// http://feedback.nventive.com/topics/257-usercontrol-doesnt-support-attached-properties/
    /// </summary>
    public partial class AttachableUserControl : UserControl
    {
    }
}
