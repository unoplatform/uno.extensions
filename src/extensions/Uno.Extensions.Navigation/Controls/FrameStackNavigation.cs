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

namespace Uno.Extensions.Navigation.Controls;

public class FrameStackNavigation : BaseControlNavigation<Frame>, IStackViewManager<Frame>
{
    public override void Inject(object control)
    {
        base.Inject(control);
        if (Control.Content is not null)
        {
            Navigation.NavigateToView(null, Control.SourcePageType);
        }
        Control.Navigated += Frame_Navigated;
    }

    private INavigationService Navigation { get; }

    public FrameStackNavigation(INavigationService navigation)
    {
        Navigation = navigation;
    }

    private void Frame_Navigated(object sender, NavigationEventArgs e)
    {
        Navigation.NavigateToView(null, Control.SourcePageType);
    }

    private void GoBack(NavigationContext context, object parameter, object viewModel)
    {
        if (parameter is not null)
        {
            var entry = Control.BackStack.Last();
            var newEntry = new PageStackEntry(entry.SourcePageType, parameter, entry.NavigationTransitionInfo);
            Control.BackStack.Remove(entry);
            Control.BackStack.Add(newEntry);
        }

        Control.GoBack();

        InitialiseView(Control.Content, context, viewModel);
    }

    public void ChangeView(NavigationContext context, bool isBackNavigation, object viewModel)
    {
        if (isBackNavigation)
        {
            GoBack(context, context.Data, viewModel);
            return;
        }

        if (context.Request.Sender is not null)
        {
            Control.Navigated -= Frame_Navigated;
            var nav = Control.Navigate(context.Mapping.View, context.Data);
            Control.Navigated += Frame_Navigated;
        }

        if (Control.Content is FrameworkElement element)
        {
            InitialiseView(Control.Content, context, viewModel);
        }
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
