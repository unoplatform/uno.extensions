using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;

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
