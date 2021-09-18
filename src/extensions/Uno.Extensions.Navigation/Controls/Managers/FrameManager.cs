using System;
using System.Diagnostics;
using System.Linq;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
#endif

namespace Uno.Extensions.Navigation.Controls.Managers;

public class FrameManager : BaseControlManager<Frame>, IStackViewManager
{
    public FrameManager(INavigationService navigation, RegionControlProvider controlProvider) : base(navigation, controlProvider.RegionControl as Frame)
    {
        if (Control.Content is not null)
        {
            Navigation.NavigateToViewAsync(null, Control.SourcePageType);
        }
        Control.Navigated += Frame_Navigated;
    }

    private void Frame_Navigated(object sender, NavigationEventArgs e)
    {
        Navigation.NavigateToViewAsync(null, Control.SourcePageType, data: e.Parameter);
    }

    public void GoBack(object parameter, object viewModel)
    {
        if (parameter is not null)
        {
            var entry = Control.BackStack.Last();
            var newEntry = new PageStackEntry(entry.SourcePageType, parameter, entry.NavigationTransitionInfo);
            Control.BackStack.Remove(entry);
            Control.BackStack.Add(newEntry);
        }

        Control.GoBack();

        InitialiseView(Control.Content, viewModel);
    }

    protected override object InternalShow(string path, Type view, object data, object viewModel)
    {
        if (Control.Content?.GetType() != view)
        {
            Control.Navigated -= Frame_Navigated;
            var nav = Control.Navigate(view, data);
            Control.Navigated += Frame_Navigated;
        }

        return Control.Content;
    }

    public void RemoveLastFromBackStack()
    {
        Control.BackStack.RemoveAt(Control.BackStack.Count - 1);
    }

    public void ClearBackStack()
    {
        Control.BackStack.Clear();
    }
}
