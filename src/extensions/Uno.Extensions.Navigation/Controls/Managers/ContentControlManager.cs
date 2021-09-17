using System;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Controls;

public class ContentControlManager : BaseControlManager<ContentControl>, IViewManager<ContentControl>
{
    public ContentControlManager(INavigationService navigation, RegionControlProvider controlProvider) : base(navigation, controlProvider.RegionControl as ContentControl)
    {
    }

    public void Show(string path, Type view, object data, object viewModel, bool setFocus)
    {
        if (setFocus)
        {
            var content = Activator.CreateInstance(view);
            Control.Content = content;
        }

        InitialiseView(Control.Content, viewModel);
    }
}
